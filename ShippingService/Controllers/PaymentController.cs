using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Data;

namespace ShippingService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PaymentController(AppDbContext db) { _db = db; }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            var list = await _db.PaymentProviders.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return Ok(list);
        }
    }
}


