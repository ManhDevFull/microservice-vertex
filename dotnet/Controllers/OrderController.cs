using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using dotnet.Dtos;
using dotnet.Service.IService;

namespace dotnet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // Yêu cầu phải đăng nhập
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
                return Unauthorized("Không thể xác định người dùng.");
            }

            try
            {
                var orders = await _orderService.GetOrderHistoryAsync(userId);
                return Ok(orders); // Trả về danh sách đơn hàng
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử đơn hàng cho User ID {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server khi lấy lịch sử đơn hàng." });
            }
        }
        [HttpGet("my-track-order")]
        public async Task<IActionResult> getMyTrackOrder()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // lấy id người dùng thông qua token
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không thể xác định người dùng");
            }
            try
            {
                var rs = await _orderService.getTrackOrder(userId);
                return Ok(rs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy track order của user {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server." });
            }
        }
        [HttpGet("my-orders/{orderId}")]
        public async Task<IActionResult> GetMyOrderDetail(int orderId)
        {
            // 1. Lấy userId từ Token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không thể xác định người dùng.");
            }

            try
            {
                // 2. Gọi Service lấy chi tiết
                var orderDetail = await _orderService.GetOrderDetailAsync(orderId);

                // 3. Kiểm tra tồn tại
                if (orderDetail == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng." });
                }

                // 4. QUAN TRỌNG: Kiểm tra bảo mật (Chỉ xem đơn của chính mình)
                if (orderDetail.accountid != userId)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng." });
                }

                // 5. Trả về
                return Ok(orderDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đơn hàng {OrderId} cho User ID {UserId}", orderId, userId);
                return StatusCode(500, new { message = "Lỗi server khi lấy chi tiết đơn hàng." });
            }
        }

    }
}
