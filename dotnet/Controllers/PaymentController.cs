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

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "PaymentController is working" });
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
                ReturnUrl = request.ReturnUrl ?? "http://localhost:3000/my-cart/customer-info/shipping-payments/product-confirm"
            };

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
}

