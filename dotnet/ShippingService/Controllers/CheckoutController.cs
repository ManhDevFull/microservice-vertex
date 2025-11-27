using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Data;
using ShippingService.Models;

namespace ShippingService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CheckoutController(AppDbContext db) { _db = db; }

        [HttpPost("selection")]
        public async Task<IActionResult> SaveSelection([FromBody] CheckoutSelection dto)
        {
            if (dto.AccountId <= 0) return BadRequest(new { message = "accountId required" });
            dto.Id = 0;
            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;
            _db.CheckoutSelections.Add(dto);
            await _db.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpGet("selection")]
        public async Task<IActionResult> GetSelection([FromQuery] int accountId)
        {
            var s = await _db.CheckoutSelections.Where(x => x.AccountId == accountId)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
            if (s == null) return NotFound();
            return Ok(s);
        }
    }
}


