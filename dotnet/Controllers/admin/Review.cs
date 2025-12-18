using System.Threading.Tasks;
using dotnet.Dtos.admin;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers.admin
{
  [Route("admin/[controller]")]
  [ApiController]
  public class ReviewController : ControllerBase
  {
    private readonly IReviewService _service;

    public ReviewController(IReviewService service)
    {
      _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> GetReviews([FromQuery] ReviewAdminQuery query)
    {
      int page = query.Page ?? 1;
      int size = query.Size ?? 20;

      var result = await _service.GetReviewsAsync(
          page,
          size,
          query.Rating,
          query.Updated,
          query.Keyword,
          query.FromDate,
          query.ToDate);

      var summary = await _service.GetSummaryAsync();

      return Ok(new
      {
        status = 200,
        data = new
        {
          items = result.Items,
          total = result.Total,
          page = result.Page,
          size = result.Size,
          summary
        },
        message = "Success"
      });
    }

    [HttpGet("{reviewId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> GetReviewDetail(int reviewId)
    {
      var review = await _service.GetReviewDetailAsync(reviewId);
      if (review == null)
      {
        return NotFound(new { status = 404, message = "Review not found" });
      }

      return Ok(new { status = 200, data = review, message = "Success" });
    }

    [HttpPut("{reviewId:int}/status")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> UpdateReviewStatus(int reviewId, [FromBody] ReviewAdminUpdateRequest request)
    {
      var updated = await _service.UpdateReviewAsync(reviewId, request.IsUpdated);
      if (!updated)
      {
        return NotFound(new { status = 404, message = "Review not found" });
      }

      return Ok(new { status = 200, message = "Updated" });
    }
  }
}
