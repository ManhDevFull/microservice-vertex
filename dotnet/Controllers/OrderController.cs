using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using dotnet.Dtos;
using dotnet.Service.IService;
using dotnet.Model;
using System.Linq;
using System.Text.Json;

namespace dotnet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("api/[controller]")]
    [Authorize] // YÃªu cáº§u pháº£i Ä‘Äƒng nháº­p
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("create-from-payment")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrderFromPayment([FromBody] CreateOrderRequestDto request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== ORDER CREATION REQUEST RECEIVED ===");
            _logger.LogInformation("OrderId={OrderId}, AccountId={AccountId}, AddressId={AddressId}, SelectedCartIds={SelectedCartIds}", 
                request?.OrderId, request?.AccountId, request?.AddressId, 
                request?.SelectedCartIds != null && request.SelectedCartIds.Count > 0 
                    ? string.Join(",", request.SelectedCartIds) 
                    : "null/empty");
            _logger.LogInformation("Request body: {Request}", System.Text.Json.JsonSerializer.Serialize(request));
            
            if (request == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest(new { error = "Request body is required." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int accountId;

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var jwtAccountId))
            {
                accountId = jwtAccountId;
                _logger.LogInformation("Using AccountId from JWT: {AccountId}", accountId);
            }
            else if (request.AccountId.HasValue)
            {
                accountId = request.AccountId.Value;
                _logger.LogInformation("Using AccountId from request: {AccountId}", accountId);
            }
            else
            {
                _logger.LogWarning("AccountId is missing from both JWT and request body");
                return Unauthorized(new { error = "AccountId is required." });
            }

            try
            {
                _logger.LogInformation("Creating order for account {AccountId} with OrderId {OrderId}", accountId, request.OrderId);
                var result = await _orderService.CreateOrdersFromCartAsync(accountId, request, cancellationToken);
                _logger.LogInformation("Order created successfully: OrderId={OrderId}, Items={Items}, CartCleared={CartCleared}", 
                    result.OrderId, result.Items, result.CartCleared);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Unable to create order for account {AccountId}: {Message}", accountId, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Address validation failed for account {AccountId}: {Message}", accountId, ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating orders from payment for account {AccountId}: {Message}, StackTrace: {StackTrace}", 
                    accountId, ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = "Internal server error." });
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrderHistory()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("KhÃ´ng thá»ƒ xÃ¡c Ä‘á»‹nh ngÆ°á»i dÃ¹ng.");
            }

            try
            {
                var orders = await _orderService.GetOrderHistoryAsync(userId);
                return Ok(orders); // Tráº£ vá» danh sÃ¡ch Ä‘Æ¡n hÃ ng
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi láº¥y lá»‹ch sá»­ Ä‘Æ¡n hÃ ng cho User ID {UserId}", userId);
                return StatusCode(500, new { message = "Lá»—i server khi láº¥y lá»‹ch sá»­ Ä‘Æ¡n hÃ ng." });
            }
        }

        [HttpGet("my-orders/{orderId}")]
        public async Task<IActionResult> GetMyOrderDetail(int orderId)
        {
            // 1. Láº¥y userId tá»« Token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("KhÃ´ng thá»ƒ xÃ¡c Ä‘á»‹nh ngÆ°á»i dÃ¹ng.");
            }

            try
            {
                // 2. Gá»i Service láº¥y chi tiáº¿t
                var orderDetail = await _orderService.GetOrderDetailAsync(orderId);

                // 3. Kiá»ƒm tra tá»“n táº¡i
                if (orderDetail == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng." });
                }

                // 4. QUAN TRá»ŒNG: Kiá»ƒm tra báº£o máº­t (Chá»‰ xem Ä‘Æ¡n cá»§a chÃ­nh mÃ¬nh)
                if (orderDetail.accountid != userId)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng." });
                }

                // 5. Return mapped detail for frontend
                var mapped = MapToOrderDetailResponse(orderDetail);
                return Ok(mapped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đơn hàng {OrderId} cho User ID {UserId}", orderId, userId);
                return StatusCode(500, new { message = "Lỗi server khi lấy chi tiết đơn hàng." });
            }
        }

        private static OrderDetailResponseDto MapToOrderDetailResponse(Order order)
        {
            var items = new List<OrderDetailItemDto>();
            decimal total = 0;

            foreach (var detail in order.orderdetails ?? Enumerable.Empty<OrderDetail>())
            {
                var quantity = detail.quantity == 0 ? 1 : detail.quantity;
                var unitPrice = detail.variant?.price ?? 0;
                var itemTotal = unitPrice * quantity;
                total += itemTotal;

                var product = detail.variant?.product;
                var attributes = ExtractAttributes(detail.variant?.valuevariant);

                items.Add(new OrderDetailItemDto
                {
                    Id = detail.id,
                    VariantId = detail.variant_id,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = itemTotal,
                    CanReview = detail.canReview,
                    Product = new OrderProductInfoDto
                    {
                        Name = product?.nameproduct ?? string.Empty,
                        Thumbnail = product?.imageurls?.FirstOrDefault() ?? string.Empty,
                        VariantAttributes = attributes
                    }
                });
            }

            var address = order.address;
            var fullAddress = string.Join(", ", new[] { address?.detail, address?.description }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            return new OrderDetailResponseDto
            {
                OrderId = order.id,
                OrderDate = order.orderdate,
                StatusOrder = order.statusorder ?? string.Empty,
                TypePay = order.typepay ?? string.Empty,
                StatusPay = order.statuspay ?? string.Empty,
                TotalPrice = total,
                TotalDiscount = 0,
                TotalPriceAfterDiscount = total,
                AddressInfo = new OrderAddressInfoDto
                {
                    Title = address?.title ?? string.Empty,
                    NameRecipient = address?.namerecipient ?? string.Empty,
                    Tel = address?.tel ?? string.Empty,
                    FullAddress = fullAddress
                },
                Items = items
            };
        }

        private static Dictionary<string, string> ExtractAttributes(JsonDocument? document)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (document == null) return result;

            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return result;

            foreach (var property in root.EnumerateObject())
            {
                result[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.TryGetDecimal(out var decVal)
                        ? decVal.ToString("G")
                        : property.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => property.Value.ToString()
                };
            }

            return result;
        }
    }
}
