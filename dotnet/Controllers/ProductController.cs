using be_dotnet_ecommerce1.Service;
using Microsoft.AspNetCore.Mvc;

namespace be_dotnet_ecommerce1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        public ProductController(IProductService service)
        {
            _service = service;
        }
        [HttpGet("{id}")]
        public IActionResult getQuantityByIdCategory(int id)
        {
            var quantity = _service.getQuantityByIdCategory(id);
            return Ok(quantity);
        }
        [HttpPost("filter")]
        public async Task<IActionResult> FilterProducts([FromBody] FilterDTO dTO)
        {
            var result = await _service.getProductByFilter(dTO);
            return Ok(result);
        }
    }
}