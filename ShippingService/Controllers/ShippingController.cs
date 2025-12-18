using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Data;

namespace ShippingService.Controllers
{
    [ApiController]
    [Route("shipping")]
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

        [HttpGet("carriers-with-options")]
        public async Task<IActionResult> GetCarriersWithOptions()
        {
            try
            {
                var carriers = await _db.ShippingCarriers
                    .Where(c => c.IsActive)
                    .Include(c => c.Options.Where(o => o.IsActive))
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                if (carriers.Count == 0)
                {
                    return Ok(DefaultCarriers());
                }

                return Ok(carriers);
            }
            catch (Exception ex)
            {
                // Return a safe default if DB fails
                return Ok(DefaultCarriers());
            }
        }

        [HttpGet("options")]
        public async Task<IActionResult> GetOptions([FromQuery] string carrierCode)
        {
            try
            {
                var carrier = await _db.ShippingCarriers.FirstOrDefaultAsync(c => c.Code == carrierCode && c.IsActive);
                if (carrier == null) return NotFound();
                var opts = await _db.ShippingOptions.Where(o => o.CarrierId == carrier.Id && o.IsActive)
                    .OrderBy(o => o.ShippingCost).ToListAsync();
                return Ok(opts);
            }
            catch
            {
                return Ok(DefaultCarriers().SelectMany(c => c.Options));
            }
        }

        private static List<ShippingService.Models.ShippingCarrier> DefaultCarriers()
        {
            var standard = new ShippingService.Models.ShippingOption
            {
                Id = 0,
                CarrierId = 0,
                Code = "STD",
                Name = "Standard",
                DeliveryMinDays = 3,
                DeliveryMaxDays = 7,
                ShippingCost = 0,
                InsuranceAvailable = true,
                Notes = "Fallback option"
            };

            return new List<ShippingService.Models.ShippingCarrier>
            {
                new ShippingService.Models.ShippingCarrier
                {
                    Id = 0,
                    Code = "DEFAULT",
                    Name = "Standard Shipping",
                    LogoUrl = string.Empty,
                    IsActive = true,
                    Options = new List<ShippingService.Models.ShippingOption> { standard }
                }
            };
        }
    }
}


