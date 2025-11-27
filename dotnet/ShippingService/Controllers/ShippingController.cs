using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Data;

namespace ShippingService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ShippingController(AppDbContext db) { _db = db; }

        [HttpGet("carriers")]
        public async Task<IActionResult> GetCarriers()
        {
            var list = await _db.ShippingCarriers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return Ok(list);
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] string carrierCode)
        {
            var carrier = await _db.ShippingCarriers.FirstOrDefaultAsync(c => c.Code == carrierCode && c.IsActive);
            if (carrier == null) return NotFound();
            var opts = await _db.ShippingOptions.Where(o => o.CarrierId == carrier.Id && o.IsActive)
                .OrderBy(o => o.ShippingCost).ToListAsync();
            return Ok(opts);
        }
    }
}


