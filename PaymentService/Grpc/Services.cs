using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Payment.Grpc;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Grpc
{
    public class PaymentRpcImpl : PaymentRpc.PaymentRpcBase
    {
        private readonly AppDbContext _db;
        private readonly MoMoService _momoService;
        private readonly ILogger<PaymentRpcImpl> _logger;

        public PaymentRpcImpl(AppDbContext db, MoMoService momoService, ILogger<PaymentRpcImpl> logger)
        {
            _db = db;
            _momoService = momoService;
            _logger = logger;
        }

        public override async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest request, ServerCallContext context)
        {
            try
            {
                // Check if order already exists
                var existing = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == request.OrderId);
                
                if (existing != null && existing.Status == "SUCCESS")
                {
                    return new CreatePaymentResponse
                    {
                        Success = false,
                        Message = "Order already paid"
                    };
                }

                // Generate unique request ID
                var requestId = Guid.NewGuid().ToString();
                
                // Prepare extraData for MoMo (selectedCartIds)
                var extraData = request.SelectedCartIds != null && request.SelectedCartIds.Count > 0
                    ? string.Join(",", request.SelectedCartIds.Distinct())
                    : string.Empty;

                // Create payment request to MoMo
                var momoResult = await _momoService.CreatePaymentAsync(
                    requestId: requestId,
                    orderId: request.OrderId,
                    amount: request.Amount,
                    orderInfo: request.OrderInfo,
                    returnUrl: request.ReturnUrl,
                    extraData: extraData
                );

                if (!momoResult.Success)
                {
                    return new CreatePaymentResponse
                    {
                        Success = false,
                        Message = momoResult.Message ?? "Failed to create payment"
                    };
                }

                // Save transaction to database
                var transaction = existing ?? new PaymentTransaction
                {
                    CreatedAt = DateTime.UtcNow
                };
                
                transaction.OrderId = request.OrderId;
                transaction.AccountId = request.AccountId;
                transaction.PartnerCode = _momoService.PartnerCode;
                transaction.RequestId = requestId;
                transaction.Amount = request.Amount;
                transaction.OrderInfo = request.OrderInfo;
                transaction.PaymentUrl = momoResult.PaymentUrl;
                transaction.QrCode = momoResult.QrCode;
                transaction.Status = "PENDING";
                transaction.SelectedCartIds = string.IsNullOrWhiteSpace(extraData) ? null : extraData;
                transaction.AddressId = request.AddressId > 0 ? request.AddressId : null;
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

                return new CreatePaymentResponse
                {
                    Success = true,
                    PaymentUrl = momoResult.PaymentUrl ?? string.Empty,
                    QrCode = momoResult.QrCode ?? string.Empty,
                    RequestId = requestId,
                    Message = "Payment created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order {OrderId}", request.OrderId);
                return new CreatePaymentResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                };
            }
        }

        public override async Task<PaymentStatusResponse> GetPaymentStatus(GetPaymentStatusRequest request, ServerCallContext context)
        {
            try
            {
                var transaction = await _db.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == request.OrderId);

                if (transaction == null)
                {
                    return new PaymentStatusResponse
                    {
                        OrderId = request.OrderId,
                        Status = "NOT_FOUND",
                        Message = "Transaction not found"
                    };
                }

                return new PaymentStatusResponse
                {
                    OrderId = transaction.OrderId,
                    Status = transaction.Status,
                    MomoTransactionId = transaction.MoMoTransactionId ?? string.Empty,
                    ResponseCode = transaction.ResponseCode ?? string.Empty,
                    Message = transaction.Message ?? string.Empty,
                    Amount = transaction.Amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for order {OrderId}", request.OrderId);
                return new PaymentStatusResponse
                {
                    OrderId = request.OrderId,
                    Status = "ERROR",
                    Message = $"Internal error: {ex.Message}"
                };
            }
        }
    }
}

