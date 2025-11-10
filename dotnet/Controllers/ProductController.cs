using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using be.Service.IService;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
