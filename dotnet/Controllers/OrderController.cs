using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
                if (orderDetail.AccountId != userId)
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