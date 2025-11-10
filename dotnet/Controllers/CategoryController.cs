using be_dotnet_ecommerce1.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace be_dotnet_ecommerce1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;
        public CategoryController(ICategoryService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> getAllCategory()
        {
            var rs = await _service.getAllCategory();
            return Ok(rs);
        }
    }
}