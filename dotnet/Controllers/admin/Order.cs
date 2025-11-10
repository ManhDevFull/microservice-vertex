using System.Threading.Tasks;
using dotnet.Dtos.admin;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers.admin
{
  [Route("admin/[controller]")]
  [ApiController]
  public class OrderController : ControllerBase
  {
    private readonly IOrderService _service;

    public OrderController(IOrderService service)
    {
      _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> GetOrders([FromQuery] OrderAdminQuery query)
    {
      int page = query.Page ?? 1;
      int size = query.Size ?? 20;

      var result = await _service.GetOrdersAsync(
          page,
          size,
          query.Status,
          query.Payment,
          query.PayType,
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

    [HttpGet("summary")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> GetSummary()
    {
      var summary = await _service.GetSummaryAsync();
      return Ok(new
      {
        status = 200,
        data = summary,
        message = "Success"
      });
    }

    [HttpGet("{orderId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> GetOrderDetail(int orderId)
    {
      var order = await _service.GetOrderDetailAsync(orderId);
      if (order == null)
      {
        return NotFound(new { status = 404, message = "Order not found" });
      }

      return Ok(new { status = 200, data = order, message = "Success" });
    }

    [HttpPut("{orderId:int}/status")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] OrderAdminUpdateStatusRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Status))
      {
        return BadRequest(new { status = 400, message = "Status is required" });
      }

      var updated = await _service.UpdateOrderStatusAsync(orderId, request.Status, request.PaymentStatus);
      if (!updated)
      {
        return NotFound(new { status = 404, message = "Order not found" });
      }

      return Ok(new { status = 200, message = "Updated" });
    }
  }
}
