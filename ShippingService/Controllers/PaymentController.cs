using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShippingService.Data;
using ShippingService.Models;

namespace ShippingService.Controllers
{
    [ApiController]
    [Route("shipping/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PaymentController(AppDbContext db) { _db = db; }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders()
        {
            try
            {
                var list = await _db.PaymentProviders.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
                if (list.Count == 0)
                {
                    return Ok(DefaultProviders());
                }
                return Ok(list);
            }
            catch
            {
                return Ok(DefaultProviders());
            }
        }

        private static List<PaymentProvider> DefaultProviders()
        {
            return new List<PaymentProvider>
            {
                new PaymentProvider
                {
                    Id = 0,
                    Code = "MOMO",
                    Name = "QR with MoMo",
                    Description = "Thanh toán nhanh qua mã QR MoMo",
                    LogoUrl = "https://vi.wikipedia.org/wiki/T%E1%BA%ADp_tin:MoMo_Logo.png",
                    IsActive = true
                }
            };
        }
    }
}


