using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using be.Service.IService;
using dotnet.Dtos;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace dotnet.Controllers
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
        [HttpPost("filter")]
        public async Task<IActionResult> FilterProducts([FromBody] FilterDTO dTO)
        {
            var result = await _service.getProductByFilter(dTO);
            return Ok(result);
        }
        [HttpGet("discount")]
        public async Task<IActionResult> getProductDiscount()
        {
            var rs = await _service.getProductsHaveDiscount();
            return Ok(rs);
        }
        [HttpGet("frequently")]
        public async Task<IActionResult> getFrequentlyProduct()
        {
            var mainProductTask = await _service.getTop1ProductByOrder();
            var accompanyingTask = await _service.getProductFrequently();


            var result = new
            {
                main = mainProductTask,
                accompanying = accompanyingTask
            };
            return Ok(result);
        }
        [HttpGet("detail-product/{id}")]
        public async Task<IActionResult> getDetail(int id)
        {
            var product = await _service.getProductById(id);
            return Ok(product);
        }
    }
}