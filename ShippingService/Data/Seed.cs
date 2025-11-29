using Microsoft.EntityFrameworkCore;
using ShippingService.Models;

namespace ShippingService.Data
{
    public static class Seed
    {
        public static async Task EnsureSeedAsync(AppDbContext db)
        {
            if (!await db.PaymentProviders.AnyAsync())
            {
                db.PaymentProviders.AddRange(
                    new PaymentProvider { Code = "paypal", Name = "PayPal", Description = "PayPal is a trusted online payment platform that allows individuals and businesses to securely send and receive money electronically.", LogoUrl = "https://cdn.paypal.com/logo-paypal.svg" },
                    new PaymentProvider { Code = "mastercard", Name = "Mastercard", Description = "Mastercard enables secure electronic payments for consumers and merchants worldwide.", LogoUrl = "https://brand.mastercard.com/mastercard.jpg" },
                    new PaymentProvider { Code = "bitcoin", Name = "Bitcoin", Description = "Pay with Bitcoin – decentralized digital currency.", LogoUrl = "https://bitcoin.org/img/icons/opengraph.png" },
                    new PaymentProvider { Code = "momo", Name = "MoMo E-Wallet", Description = "MoMo is a leading Vietnamese e-wallet for fast, convenient payments and money transfers.", LogoUrl = "https://upload.wikimedia.org/wikipedia/vi/f/fe/MoMo_Logo.png" }
                );
            }

            if (!await db.ShippingCarriers.AnyAsync())
            {
                var ausff = new ShippingCarrier { Code = "ausff", Name = "AUSFF", LogoUrl = "https://example.com/logos/ausff.png" };
                var race = new ShippingCarrier { Code = "racecouriers", Name = "RaceCouriers", LogoUrl = "https://example.com/logos/racecouriers.png" };
                var trans = new ShippingCarrier { Code = "transcocargo", Name = "TranscoCargo", LogoUrl = "https://example.com/logos/transcocargo.png" };
                db.ShippingCarriers.AddRange(ausff, race, trans);
                await db.SaveChangesAsync();

                db.ShippingOptions.AddRange(
                    new ShippingOption { CarrierId = ausff.Id, Code = "standard", Name = "Standard 14-21 days", DeliveryMinDays = 14, DeliveryMaxDays = 21, ShippingCost = 0, InsuranceAvailable = false, Notes = "Shipping cost: Free. Insurance: Unavailable." },
                    new ShippingOption { CarrierId = race.Id, Code = "standard", Name = "Standard 14-21 days", DeliveryMinDays = 14, DeliveryMaxDays = 21, ShippingCost = 10, InsuranceAvailable = true, Notes = "Shipping cost: ₹10. Insurance available." },
                    new ShippingOption { CarrierId = trans.Id, Code = "standard", Name = "Standard 14-21 days", DeliveryMinDays = 14, DeliveryMaxDays = 21, ShippingCost = 12, InsuranceAvailable = true, Notes = "Shipping cost: ₹12. Insurance available." }
                );
            }

            await db.SaveChangesAsync();
        }
    }
}


