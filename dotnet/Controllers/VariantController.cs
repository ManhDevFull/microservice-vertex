using System.Text.Json;
using be_dotnet_ecommerce1.Service.IService;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace be_dotnet_ecommerce1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VariantController : ControllerBase
    {
        private readonly IVariantService _service;
        public VariantController(IVariantService service)
        {
            _service = service;
        }
        // [HttpGet]
        // public async Task<IActionResult> getValueVariant()
        // {
        //     var list = await _service.getValueVariant();
        //     return Ok(list);
        // }
        [HttpGet("getAllVariant")]
        public async Task<IActionResult> getAllVariant()
        {
            var rs = await _service.getAllVariant();
            return Ok(rs);
        }

        [HttpGet]
        public async Task<IActionResult> getValueVarianByCategory(string? name){
            var rs = await _service.getValueVariantByNameCategory(name);
            return Ok(rs);
        }
    }
}