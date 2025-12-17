using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using be.Service.IService;
using dotnet.Model;

namespace dotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly ConnectData _context;
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
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("Không xác định được người dùng.");
                }

                var orderDetail = await _context.orderdetails
                    .Include(od => od.order)
                    .FirstOrDefaultAsync(od => od.id == request.OrderDetailId);

                if (orderDetail == null)
                {
                    return NotFound("Không tìm thấy sản phẩm này trong đơn hàng.");
                }

                if (orderDetail.order?.accountid != userId)
                {
                    return Forbid("Bạn không có quyền đánh giá đơn hàng của người khác.");
                }

                var status = orderDetail.order.statusorder?.Trim().ToUpperInvariant() ?? string.Empty;
                if (status != "DELIVERED")
                {
                    return BadRequest("Only allowed to review after the order is delivered.");
                }

                var existingReview = await _context.reviews
                    .FirstOrDefaultAsync(r => r.orderdetail_id == request.OrderDetailId);

                if (existingReview != null)
                {
                    return BadRequest("Bạn đã đánh giá sản phẩm này rồi.");
                }

                var uploadedUrls = new List<string>();
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

                _context.reviews.Add(newReview);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đánh giá thành công!", reviewId = newReview.id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo review");
                return StatusCode(500, "Lỗi server: " + ex.Message);
            }
        }

        [HttpGet("by-order-detail/{orderDetailId:int}")]
        public async Task<IActionResult> GetReviewByOrderDetail(int orderDetailId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            var review = await _context.reviews
                .Include(r => r.orderdetail)
                    .ThenInclude(od => od.order)
                .FirstOrDefaultAsync(r => r.orderdetail_id == orderDetailId);

            if (review == null)
            {
                return NotFound(new { message = "Review không tồn tại." });
            }

            if (review.orderdetail.order?.accountid != userId)
            {
                return Forbid();
            }

            return Ok(new
            {
                reviewId = review.id,
                rating = review.rating,
                content = review.content ?? string.Empty,
                images = review.imageurls ?? Array.Empty<string>(),
                createDate = review.createdate,
                updateDate = review.updatedate,
                isUpdated = review.isupdated
            });
        }

        [HttpPut("{reviewId:int}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromForm] UpdateReviewRequest request)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("Không xác định được người dùng.");
                }

                var review = await _context.reviews
                    .Include(r => r.orderdetail)
                        .ThenInclude(od => od.order)
                    .FirstOrDefaultAsync(r => r.id == reviewId);

                if (review == null)
                {
                    return NotFound("Không tìm thấy review.");
                }

                if (review.orderdetail.order?.accountid != userId)
                {
                    return Forbid("Bạn không có quyền sửa review này.");
                }

                review.rating = request.Rating;
                review.content = request.Content;
                review.isupdated = true;
                review.updatedate = DateTime.UtcNow;

                if (request.Images != null && request.Images.Count > 0)
                {
                    var uploadedUrls = new List<string>();
                    foreach (var file in request.Images)
                    {
                        var uploadResult = await _photoService.AddPhotoAsync(file);
                        if (uploadResult.Error == null)
                        {
                            uploadedUrls.Add(uploadResult.SecureUrl.ToString());
                        }
                    }
                    review.imageurls = uploadedUrls.ToArray();
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật review thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật review");
                return StatusCode(500, "Lỗi server: " + ex.Message);
            }
        }
    }
}
