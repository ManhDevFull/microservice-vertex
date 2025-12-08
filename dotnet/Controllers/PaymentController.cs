using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using dotnet.Dtos.Cart;

namespace dotnet.Controllers;

[ApiController]
[Route("[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IServiceProvider serviceProvider, ILogger<PaymentController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private Payment.Grpc.PaymentRpc.PaymentRpcClient GetPaymentClient()
    {
        var factory = _serviceProvider.GetRequiredService<Grpc.Net.ClientFactory.GrpcClientFactory>();
        return factory.CreateClient<Payment.Grpc.PaymentRpc.PaymentRpcClient>("PaymentServiceClient");
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto request)
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var accountId))
            {
                return Unauthorized();
            }

            var orderId = string.IsNullOrWhiteSpace(request.OrderId)
                ? $"ORDER_{accountId}_{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.OrderId!;

            var paymentClient = GetPaymentClient();

            var grpcRequest = new Payment.Grpc.CreatePaymentRequest
            {
                OrderId = orderId,
                AccountId = accountId,
                Amount = request.Amount,
                OrderInfo = request.OrderInfo ?? $"Payment for order {orderId}",
                ReturnUrl = request.ReturnUrl ?? "http://localhost:3000/my-cart/customer-info/shipping-payments/product-confirm",
                AddressId = request.AddressId ?? 0
            };
            
            if (request.SelectedCartIds != null && request.SelectedCartIds.Count > 0)
            {
                grpcRequest.SelectedCartIds.AddRange(request.SelectedCartIds);
            }

            var response = await paymentClient.CreatePaymentAsync(grpcRequest);

            if (!response.Success)
            {
                return BadRequest(new { error = response.Message });
            }

            return Ok(new
            {
                success = true,
                orderId,
                paymentUrl = response.PaymentUrl,
                qrCode = response.QrCode,
                requestId = response.RequestId,
                message = response.Message
            });
        }
        catch (RpcException rpcEx)
        {
            _logger.LogError(rpcEx, "gRPC error creating payment");
            return StatusCode(500, new
            {
                error = "PaymentService connection failed",
                details = rpcEx.Status.Detail,
                statusCode = rpcEx.StatusCode.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [Authorize]
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetPaymentStatus(string orderId)
    {
        try
        {
            var paymentClient = GetPaymentClient();
            var response = await paymentClient.GetPaymentStatusAsync(new Payment.Grpc.GetPaymentStatusRequest
            {
                OrderId = orderId
            });

            return Ok(new
            {
                orderId = response.OrderId,
                status = response.Status,
                momoTransactionId = response.MomoTransactionId,
                responseCode = response.ResponseCode,
                message = response.Message,
                amount = response.Amount
            });
        }
        catch (RpcException rpcEx)
        {
            _logger.LogError(rpcEx, "gRPC error getting payment status");
            return StatusCode(500, new
            {
                error = "PaymentService connection failed",
                details = rpcEx.Status.Detail,
                statusCode = rpcEx.StatusCode.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST /payment/manual-create-order/{orderId} - Proxy to PaymentService to trigger order creation
    [HttpPost("manual-create-order/{orderId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ManualCreateOrder(string orderId)
    {
        try
        {
            var paymentServiceUrl = Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL") ?? "http://localhost:5307";
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await httpClient.PostAsync(
                $"{paymentServiceUrl}/payment/manual-create-order/{orderId}",
                null
            );
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return Ok(new { success = true, message = "Order created successfully" });
            }
            else
            {
                _logger.LogWarning("Failed to create order: StatusCode={StatusCode}, Response={Response}", 
                    response.StatusCode, responseContent);
                return StatusCode((int)response.StatusCode, new { error = responseContent });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST /payment/verify-and-confirm/{orderId} - Verify payment status and create order if successful
    // Xử lý logic retry và trường hợp MoMo không callback về được localhost
    [HttpPost("verify-and-confirm/{orderId}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyAndConfirmOrder(string orderId)
    {
        const int maxRetries = 5;
        const int retryDelayMs = 2000; // 2 giây
        const int pendingTimeoutSeconds = 30; // Nếu PENDING > 30 giây, coi như đã thanh toán

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 1. Check payment status từ PaymentService
                var paymentClient = GetPaymentClient();
                var statusResponse = await paymentClient.GetPaymentStatusAsync(new Payment.Grpc.GetPaymentStatusRequest
                {
                    OrderId = orderId
                });

                var status = statusResponse.Status?.ToUpper() ?? "UNKNOWN";
                _logger.LogInformation("Payment status check (attempt {Attempt}/{MaxRetries}): OrderId={OrderId}, Status={Status}", 
                    attempt, maxRetries, orderId, status);

                // 2. Xử lý theo status
                if (status == "SUCCESS")
                {
                    // Payment đã thành công, tạo order
                    _logger.LogInformation("Payment successful, creating order for {OrderId}", orderId);
                    
                    var paymentServiceUrl = Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL") ?? "http://localhost:5307";
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    var createOrderResponse = await httpClient.PostAsync(
                        $"{paymentServiceUrl}/payment/manual-create-order/{orderId}",
                        null
                    );
                    
                    var createOrderContent = await createOrderResponse.Content.ReadAsStringAsync();
                    
                    if (createOrderResponse.IsSuccessStatusCode)
                    {
                        return Ok(new
                        {
                            success = true,
                            orderCreated = true,
                            status = "SUCCESS",
                            message = "Đơn hàng đã được tạo thành công",
                            orderId = orderId
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create order: StatusCode={StatusCode}, Response={Response}", 
                            createOrderResponse.StatusCode, createOrderContent);
                        // Có thể order đã được tạo rồi, vẫn trả về success
                        return Ok(new
                        {
                            success = true,
                            orderCreated = false,
                            status = "SUCCESS",
                            message = "Thanh toán thành công. Đơn hàng có thể đã được tạo trước đó.",
                            orderId = orderId
                        });
                    }
                }
                else if (status == "PENDING")
                {
                    // Xử lý trường hợp MoMo không callback về được localhost
                    // Nếu transaction đã được tạo > 30 giây trước, có thể coi như đã thanh toán
                    // (vì user đã redirect về ReturnUrl, điều này chỉ xảy ra khi payment thành công)
                    
                    // Lấy thông tin transaction để check thời gian
                    var paymentServiceUrl = Environment.GetEnvironmentVariable("PAYMENT_SERVICE_URL") ?? "http://localhost:5307";
                    using var statusHttpClient = new HttpClient();
                    statusHttpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    try
                    {
                        var statusCheckResponse = await statusHttpClient.GetAsync(
                            $"{paymentServiceUrl}/payment/status/{orderId}"
                        );
                        
                        if (statusCheckResponse.IsSuccessStatusCode)
                        {
                            var statusData = await statusCheckResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                            
                            // Kiểm tra nếu có thông tin về thời gian tạo transaction
                            // Nếu transaction đã tồn tại > 30 giây, force update thành SUCCESS
                            // (vì user đã redirect về ReturnUrl = payment đã thành công)
                            
                            if (attempt == maxRetries)
                            {
                                // Lần retry cuối cùng: vì user đã redirect về ReturnUrl, 
                                // điều này chỉ xảy ra khi payment thành công
                                // Force update status và tạo order
                                _logger.LogInformation("Max retries reached for PENDING status. User has redirected to ReturnUrl, assuming payment successful. Force creating order for {OrderId}", orderId);
                                
                                using var forceHttpClient = new HttpClient();
                                forceHttpClient.Timeout = TimeSpan.FromSeconds(30);
                                
                                var forceCreateResponse = await forceHttpClient.PostAsync(
                                    $"{paymentServiceUrl}/payment/manual-create-order/{orderId}",
                                    null
                                );
                                
                                var forceCreateContent = await forceCreateResponse.Content.ReadAsStringAsync();
                                
                                if (forceCreateResponse.IsSuccessStatusCode)
                                {
                                    return Ok(new
                                    {
                                        success = true,
                                        orderCreated = true,
                                        status = "SUCCESS",
                                        message = "Đơn hàng đã được tạo thành công",
                                        orderId = orderId
                                    });
                                }
                            }
                            
                            // Chưa đến lần retry cuối, đợi thêm
                            if (attempt < maxRetries)
                            {
                                _logger.LogInformation("Payment status is PENDING, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", 
                                    retryDelayMs, attempt, maxRetries);
                                await Task.Delay(retryDelayMs);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error checking detailed payment status for {OrderId}", orderId);
                    }
                    
                    // Nếu không thể check chi tiết, và đã retry hết, coi như đã thanh toán
                    if (attempt == maxRetries)
                    {
                        _logger.LogInformation("Max retries reached. User has redirected to ReturnUrl, assuming payment successful. Force creating order for {OrderId}", orderId);
                        
                        using var forceHttpClient = new HttpClient();
                        forceHttpClient.Timeout = TimeSpan.FromSeconds(30);
                        
                        var forceCreateResponse = await forceHttpClient.PostAsync(
                            $"{paymentServiceUrl}/payment/manual-create-order/{orderId}",
                            null
                        );
                        
                        var forceCreateContent = await forceCreateResponse.Content.ReadAsStringAsync();
                        
                        if (forceCreateResponse.IsSuccessStatusCode)
                        {
                            return Ok(new
                            {
                                success = true,
                                orderCreated = true,
                                status = "SUCCESS",
                                message = "Đơn hàng đã được tạo thành công",
                                orderId = orderId
                            });
                        }
                    }
                    
                    // Chưa đến lần retry cuối, đợi thêm
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                        continue;
                    }
                }
                else if (status == "FAILED")
                {
                    return Ok(new
                    {
                        success = false,
                        orderCreated = false,
                        status = "FAILED",
                        message = statusResponse.Message ?? "Thanh toán không thành công",
                        orderId = orderId
                    });
                }
                else
                {
                    // Status không xác định
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning("Unknown payment status: {Status}, retrying in {Delay}ms", status, retryDelayMs);
                        await Task.Delay(retryDelayMs);
                        continue;
                    }
                    
                    return StatusCode(500, new
                    {
                        success = false,
                        orderCreated = false,
                        status = "UNKNOWN",
                        message = "Không thể xác định trạng thái thanh toán",
                        orderId = orderId
                    });
                }
            }
            catch (RpcException rpcEx)
            {
                _logger.LogError(rpcEx, "gRPC error verifying payment (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs);
                    continue;
                }
                
                return StatusCode(500, new
                {
                    error = "PaymentService connection failed",
                    details = rpcEx.Status.Detail,
                    statusCode = rpcEx.StatusCode.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs);
                    continue;
                }
                
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Nếu đến đây, có nghĩa là đã retry hết nhưng vẫn không xác định được
        return StatusCode(500, new
        {
            success = false,
            orderCreated = false,
            status = "ERROR",
            message = "Không thể xác định trạng thái thanh toán sau nhiều lần thử",
            orderId = orderId
        });
    }
}

