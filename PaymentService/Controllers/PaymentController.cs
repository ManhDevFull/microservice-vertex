using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Services;
using PaymentService.Models;
using PaymentService.Dtos;
using Newtonsoft.Json;
using System.Net.Http;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly MoMoService _momoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public PaymentController(
            AppDbContext db, 
            MoMoService momoService, 
            ILogger<PaymentController> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _db = db;
            _momoService = momoService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // POST /payment/create - create MoMo payment
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required" });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount must be greater than zero" });
            }

            var orderId = string.IsNullOrWhiteSpace(request.OrderId)
                ? Guid.NewGuid().ToString("N")
                : request.OrderId.Trim();

            var accountId = request.AccountId ?? 0;
            var orderInfo = string.IsNullOrWhiteSpace(request.OrderInfo)
                ? $"Order {orderId}"
                : request.OrderInfo.Trim();

            var existing = await _db.PaymentTransactions
                .FirstOrDefaultAsync(t => t.OrderId == orderId);

            if (existing != null && existing.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new
                {
                    success = false,
                    message = "Order already paid",
                    orderId = existing.OrderId,
                    paymentUrl = existing.PaymentUrl,
                    requestId = existing.RequestId
                });
            }

            var requestId = Guid.NewGuid().ToString("N");
            var extraData = request.SelectedCartIds != null && request.SelectedCartIds.Count > 0
                ? string.Join(",", request.SelectedCartIds.Distinct())
                : string.Empty;

            var momoResult = await _momoService.CreatePaymentAsync(
                requestId: requestId,
                orderId: orderId,
                amount: request.Amount,
                orderInfo: orderInfo,
                returnUrl: request.ReturnUrl ?? string.Empty,
                extraData: extraData
            );

            if (!momoResult.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = momoResult.Message ?? "Failed to create payment"
                });
            }

            var transaction = existing ?? new PaymentTransaction
            {
                CreatedAt = DateTime.UtcNow
            };

            transaction.OrderId = orderId;
            transaction.AccountId = accountId;
            transaction.PartnerCode = _momoService.PartnerCode;
            transaction.RequestId = requestId;
            transaction.Amount = request.Amount;
            transaction.OrderInfo = orderInfo;
            transaction.PaymentUrl = momoResult.PaymentUrl;
            transaction.QrCode = momoResult.QrCode;
            transaction.Status = "PENDING";
            transaction.SelectedCartIds = string.IsNullOrWhiteSpace(extraData) ? null : extraData;
            transaction.AddressId = request.AddressId;
            transaction.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                _db.PaymentTransactions.Add(transaction);
            }
            else
            {
                _db.PaymentTransactions.Update(transaction);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                orderId,
                paymentUrl = momoResult.PaymentUrl ?? string.Empty,
                qrCode = momoResult.QrCode ?? string.Empty,
                requestId,
                message = momoResult.Message ?? "Payment created successfully"
            });
        }

        // POST /payment/callback - MoMo IPN (Instant Payment Notification)
        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] MoMoCallbackRequest request)
        {
            _logger.LogInformation("MoMo callback received: OrderId={OrderId}, ResultCode={ResultCode}, Amount={Amount}", 
                request?.orderId, request?.resultCode, request?.amount);
            
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("MoMo callback request is null");
                    return BadRequest(new { error = "Request body is required" });
                }

                // Verify signature
                var rawData = $"accessKey={_momoService.AccessKey}&amount={request.amount}&extraData={request.extraData}&message={request.message}&orderId={request.orderId}&orderInfo={request.orderInfo}&orderType={request.orderType}&partnerCode={request.partnerCode}&payType={request.payType}&requestId={request.requestId}&responseTime={request.responseTime}&resultCode={request.resultCode}&transId={request.transId}";
                if (!_momoService.VerifySignature(rawData, request.signature))
                {
                    _logger.LogWarning("Invalid signature in MoMo callback for order {OrderId}", request.orderId);
                    return BadRequest(new { error = "Invalid signature" });
                }

                // Find transaction
                var transaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == request.orderId);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction not found for order {OrderId}", request.orderId);
                    return NotFound(new { error = "Transaction not found" });
                }

                // Update transaction status
                transaction.MoMoTransactionId = request.transId.ToString();
                transaction.ResponseCode = request.resultCode.ToString();
                transaction.Message = request.message;
                transaction.Signature = request.signature;
                transaction.UpdatedAt = DateTime.UtcNow;

                if (request.resultCode == 0)
                {
                    transaction.Status = "SUCCESS";
                    transaction.PaidAt = DateTime.UtcNow;
                    _logger.LogInformation("Payment successful for order {OrderId}, transaction {TransId}", request.orderId, request.transId);
                    
                    // Call main backend to create orders
                    try
                    {
                        var mainBackendUrl = _config["MainBackend:Url"] ?? "http://localhost:5000";
                        var httpClient = _httpClientFactory.CreateClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(30);
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "PaymentService/1.0");
                        
                        var selectedIds = new List<int>();
                        var rawIds = !string.IsNullOrWhiteSpace(transaction.SelectedCartIds)
                            ? transaction.SelectedCartIds
                            : request.extraData;
                        if (!string.IsNullOrWhiteSpace(rawIds))
                        {
                            selectedIds = rawIds
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(idStr => int.TryParse(idStr, out var id) ? id : 0)
                                .Where(id => id > 0)
                                .ToList();
                        }

                        var createOrderRequest = new
                        {
                            OrderId = request.orderId,
                            AccountId = transaction.AccountId,
                            AddressId = transaction.AddressId > 0 ? transaction.AddressId : (int?)null,
                            PaymentMethod = "MoMo",
                            ClearCart = true,
                            SelectedCartIds = selectedIds.Count > 0 ? selectedIds : null
                        };
                        
                        var json = JsonConvert.SerializeObject(createOrderRequest);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync($"{mainBackendUrl}/Order/create-from-payment", content);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Successfully created orders for order {OrderId}", request.orderId);
                        }
                        else
                        {
                            _logger.LogError("Failed to create orders: StatusCode={StatusCode}, Response={Response}", 
                                response.StatusCode, responseContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling main backend to create orders for order {OrderId}", request.orderId);
                    }
                }
                else
                {
                    transaction.Status = "FAILED";
                    _logger.LogWarning("Payment failed for order {OrderId}: {Message}", request.orderId, request.message);
                }

                await _db.SaveChangesAsync();

                // Return success response to MoMo
                return Ok(new
                {
                    resultCode = 0,
                    message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo callback");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST /payment/manual-create-order/{orderId} - Trigger order creation when MoMo callback cannot reach localhost
        [HttpPost("manual-create-order/{orderId}")]
        public async Task<IActionResult> ManualCreateOrder(string orderId)
        {
            try
            {
                _logger.LogInformation("Manual order creation requested for order {OrderId}", orderId);
                
                var transaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == orderId);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction not found for order {OrderId}", orderId);
                    return NotFound(new { error = "Transaction not found" });
                }

                // Update transaction status to SUCCESS
                transaction.Status = "SUCCESS";
                transaction.PaidAt = DateTime.UtcNow;
                transaction.MoMoTransactionId = transaction.MoMoTransactionId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                transaction.ResponseCode = "0";
                transaction.Message = "Order created via manual trigger";
                transaction.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Call main backend to create orders
                try
                {
                    var mainBackendUrl = _config["MainBackend:Url"] ?? "http://localhost:5000";
                    var httpClient = _httpClientFactory.CreateClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "PaymentService/1.0");
                    
                    var selectedIds = new List<int>();
                    if (!string.IsNullOrWhiteSpace(transaction.SelectedCartIds))
                    {
                        selectedIds = transaction.SelectedCartIds
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(idStr => int.TryParse(idStr, out var id) ? id : 0)
                            .Where(id => id > 0)
                            .ToList();
                    }

                    var createOrderRequest = new
                    {
                        OrderId = transaction.OrderId,
                        AccountId = transaction.AccountId,
                        AddressId = transaction.AddressId > 0 ? transaction.AddressId : (int?)null,
                        PaymentMethod = "MoMo",
                        ClearCart = true,
                        SelectedCartIds = selectedIds.Count > 0 ? selectedIds : null
                    };
                    
                    var json = JsonConvert.SerializeObject(createOrderRequest);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"{mainBackendUrl}/Order/create-from-payment", content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully created orders for order {OrderId}", orderId);
                        return Ok(new { 
                            success = true, 
                            message = "Order created successfully",
                            orderId = orderId
                        });
                    }
                    else
                    {
                        _logger.LogError("Failed to create orders: StatusCode={StatusCode}, Response={Response}", 
                            response.StatusCode, responseContent);
                        return StatusCode(500, new { 
                            error = "Failed to create order in main backend",
                            statusCode = response.StatusCode.ToString(),
                            details = responseContent 
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling main backend to create orders for order {OrderId}", orderId);
                    return StatusCode(500, new { error = $"Error calling main backend: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually creating order for {OrderId}", orderId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /payment/status/{orderId} - Check payment status
        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetStatus(string orderId)
        {
            try
            {
                var transaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == orderId);

                if (transaction == null)
                {
                    return NotFound(new { error = "Transaction not found" });
                }

                return Ok(new
                {
                    orderId = transaction.OrderId,
                    status = transaction.Status,
                    amount = transaction.Amount,
                    momoTransactionId = transaction.MoMoTransactionId,
                    responseCode = transaction.ResponseCode,
                    message = transaction.Message,
                    paidAt = transaction.PaidAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

