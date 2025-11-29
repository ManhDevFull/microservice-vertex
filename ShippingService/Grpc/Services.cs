using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Shipping.Grpc;
using ShippingService.Data;

namespace ShippingService.GrpcImpl
{
    public class PaymentRpcImpl : PaymentRpc.PaymentRpcBase
    {
        private readonly AppDbContext _db;
        public PaymentRpcImpl(AppDbContext db) { _db = db; }

        public override async Task<PaymentProviderList> GetProviders(Empty request, ServerCallContext context)
        {
            var list = await _db.PaymentProviders.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var res = new PaymentProviderList();
            res.Items.AddRange(list.Select(p => new PaymentProviderDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                LogoUrl = p.LogoUrl ?? string.Empty
            }));
            return res;
        }
    }

    public class ShippingRpcImpl : ShippingRpc.ShippingRpcBase
    {
        private readonly AppDbContext _db;
        public ShippingRpcImpl(AppDbContext db) { _db = db; }

        public override async Task<ShippingCarrierList> GetCarriers(Empty request, ServerCallContext context)
        {
            var list = await _db.ShippingCarriers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var res = new ShippingCarrierList();
            res.Items.AddRange(list.Select(c => new ShippingCarrierDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                LogoUrl = c.LogoUrl ?? string.Empty
            }));
            return res;
        }

        public override async Task<ShippingOptionList> GetOptions(GetOptionsRequest request, ServerCallContext context)
        {
            var carrier = await _db.ShippingCarriers.FirstOrDefaultAsync(c => c.Code == request.CarrierCode && c.IsActive);
            if (carrier == null) return new ShippingOptionList();
            var opts = await _db.ShippingOptions.Where(o => o.CarrierId == carrier.Id && o.IsActive)
                .OrderBy(o => o.ShippingCost).ToListAsync();
            var res = new ShippingOptionList();
            res.Items.AddRange(opts.Select(o => new ShippingOptionDto
            {
                Id = o.Id,
                CarrierId = o.CarrierId,
                Code = o.Code,
                Name = o.Name,
                DeliveryMinDays = o.DeliveryMinDays ?? 0,
                DeliveryMaxDays = o.DeliveryMaxDays ?? 0,
                ShippingCost = (double)o.ShippingCost,
                InsuranceAvailable = o.InsuranceAvailable,
                Notes = o.Notes ?? string.Empty
            }));
            return res;
        }
    }

    public class CheckoutRpcImpl : CheckoutRpc.CheckoutRpcBase
    {
        private readonly AppDbContext _db;
        public CheckoutRpcImpl(AppDbContext db) { _db = db; }

        public override async Task<SaveSelectionResponse> SaveSelection(CheckoutSelectionDto request, ServerCallContext context)
        {
            if (request.AccountId <= 0) throw new RpcException(new Status(StatusCode.InvalidArgument, "accountId required"));
            var entity = new ShippingService.Models.CheckoutSelection
            {
                AccountId = request.AccountId,
                PaymentProviderId = request.PaymentProviderId,
                ShippingOptionId = request.ShippingOptionId,
                AddressSnapshot = request.AddressSnapshot,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.CheckoutSelections.Add(entity);
            await _db.SaveChangesAsync();
            return new SaveSelectionResponse { Id = entity.Id };
        }

        public override async Task<CheckoutSelectionDto> GetSelection(GetSelectionRequest request, ServerCallContext context)
        {
            var s = await _db.CheckoutSelections.Where(x => x.AccountId == request.AccountId)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
            if (s == null) throw new RpcException(new Status(StatusCode.NotFound, "not found"));
            return new CheckoutSelectionDto
            {
                Id = s.Id,
                AccountId = s.AccountId,
                PaymentProviderId = s.PaymentProviderId,
                ShippingOptionId = s.ShippingOptionId,
                AddressSnapshot = s.AddressSnapshot
            };
        }
    }
}


