using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Services;
using PaymentService.Models;
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

        // GET /payment/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "PaymentService is working" });
        }

        // POST /payment/callback - MoMo IPN (Instant Payment Notification)
        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] MoMoCallbackRequest request)
        {
            try
            {
                _logger.LogInformation("Received MoMo callback: {OrderId}, {ResultCode}", request.orderId, request.resultCode);

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
                        
                        var createOrderRequest = new
                        {
                            OrderId = request.orderId,
                            AccountId = transaction.AccountId, // Send accountId from transaction
                            PaymentMethod = "MoMo",
                            ClearCart = true // Explicitly clear cart after order creation
                        };
                        
                        var json = JsonConvert.SerializeObject(createOrderRequest);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        _logger.LogInformation("Calling main backend to create order: {Url}, Request: {Request}", 
                            $"{mainBackendUrl}/Order/create-from-payment", json);
                        
                        var response = await httpClient.PostAsync($"{mainBackendUrl}/Order/create-from-payment", content);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Successfully created orders in main backend for order {OrderId}. Response: {Response}", 
                                request.orderId, responseContent);
                        }
                        else
                        {
                            _logger.LogError("Failed to create orders in main backend: {StatusCode}, {Error}. Request was: {Request}", 
                                response.StatusCode, responseContent, json);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the callback - payment is already successful
                        _logger.LogError(ex, "Error calling main backend to create orders for order {OrderId}. Exception: {Message}, StackTrace: {StackTrace}", 
                            request.orderId, ex.Message, ex.StackTrace);
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

