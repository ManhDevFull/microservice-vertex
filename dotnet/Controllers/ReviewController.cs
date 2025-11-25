using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_dotnet_ecommerce1.Data;
using dotnet.Model;
using be_dotnet_ecommerce1.Dtos;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using be.Service.IService;
namespace dotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly ConnectData _context; // DbContext
        private readonly IPhotoService _photoService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ConnectData context, IPhotoService photoService, ILogger<ReviewController> logger)
        {
            _context = context;
            _photoService = photoService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateReview([FromForm] CreateReviewRequest request)
        {
            try
            {
                // 1. Lấy User ID từ Token
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("Không xác định được người dùng.");
                }

                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == request.OrderDetailId);

                if (orderDetail == null)
                {
                    return NotFound("Không tìm thấy sản phẩm này trong đơn hàng.");
                }

                if (orderDetail.Order.AccountId != userId) 
                {
                    return Forbid("Bạn không có quyền đánh giá đơn hàng của người khác.");
                }
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.orderdetail_id == request.OrderDetailId);
                
                if (existingReview != null)
                {
                    return BadRequest("Bạn đã đánh giá sản phẩm này rồi.");
                }

                List<string> uploadedUrls = new List<string>();

                if (request.Images != null && request.Images.Count > 0)
                {
                    foreach (var file in request.Images)
                    {
                        var uploadResult = await _photoService.AddPhotoAsync(file);
                        if (uploadResult.Error == null)
                        {
                            uploadedUrls.Add(uploadResult.SecureUrl.ToString());
                        }
                    }
                }

                var newReview = new Review
                {
                    orderdetail_id = request.OrderDetailId,
                    rating = request.Rating,
                    content = request.Content,
                    imageurls = uploadedUrls.ToArray(), 
                    createdate = DateTime.UtcNow,
                    isupdated = false
                };

                _context.Reviews.Add(newReview);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đánh giá thành công!", reviewId = newReview.id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo review");
                return StatusCode(500, "Lỗi server: " + ex.Message);
            }
        }
    }
}