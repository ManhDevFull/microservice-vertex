using System;
using System.Linq;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Service.IService;
using dotnet.Dtos.admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers.admin
{
  [Route("admin/[controller]")]
  [ApiController]
  public class CategoryController : ControllerBase
  {
    private readonly ICategoryService _service;
    public CategoryController(ICategoryService service)
    {
      _service = service;
    }
    [HttpGet]
    [Authorize(Roles = "0")]
    public IActionResult CategoryParentAdmin()
    {
      var list = _service.getCategoryAdmin();
      return Ok(new
      {
        status = 200,
        data = list,
        message = "Success"
      });
    }

    [HttpGet("brand")]
    [Authorize(Roles = "0")]
    public IActionResult GetBrandByCate([FromQuery(Name = "cate")] int? categoryId)
    {
      var list = _service.getBrandByCate(categoryId);
      return Ok(new
      {
        status = 200,
        data = list,
        message = "Success"
      });
    }

    [HttpPost]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateRequest request)
    {
      if (!ModelState.IsValid)
      {
        var error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        return BadRequest(new { status = 400, message = string.IsNullOrWhiteSpace(error) ? "Invalid payload" : error });
      }

      try
      {
        var created = await _service.CreateCategoryAsync(request);
        return StatusCode(StatusCodes.Status201Created, new
        {
          status = 201,
          data = created,
          message = "Created"
        });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        return Conflict(new { status = 409, message = ex.Message });
      }
    }

    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] CategoryUpdateRequest request)
    {
      try
      {
        var updated = await _service.UpdateCategoryAsync(categoryId, request);
        if (updated == null)
        {
          return NotFound(new { status = 404, message = "Category not found" });
        }
        return Ok(new { status = 200, data = updated, message = "Updated" });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        return Conflict(new { status = 409, message = ex.Message });
      }
    }

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
      try
      {
        var deleted = await _service.DeleteCategoryAsync(categoryId);
        if (!deleted)
        {
          return NotFound(new { status = 404, message = "Category not found" });
        }
        return Ok(new { status = 200, message = "Deleted" });
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
    }
  }
}
