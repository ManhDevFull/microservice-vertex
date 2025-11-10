using be_dotnet_ecommerce1.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
  [Route("[controller]")]
  [ApiController]
  public class CategoryController : ControllerBase
  {
    private readonly ICategoryService _service;
    public CategoryController(ICategoryService service)
    {
      _service = service;
    }
    [HttpGet("parent/{id?}")]
    public IActionResult CategoryParent(int? id)
    {
      var list = _service.getCategoryParentById(id);
      return Ok(list);
    }
    // [HttpGet("/admin/category")]
    // [Authorize(Roles = "0")]
    // public IActionResult CategoryParentAdmin(int? id)
    // {
    //   var list = _service.getCategoryParentById(id);
    //   return Ok(list);
    // }
  }
}
