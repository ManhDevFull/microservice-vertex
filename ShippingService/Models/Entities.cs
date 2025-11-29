namespace ShippingService.Models
{
    public class PaymentProvider
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ShippingCarrier
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<ShippingOption> Options { get; set; } = new();
    }

    public class ShippingOption
    {
        public int Id { get; set; }
        public int CarrierId { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int? DeliveryMinDays { get; set; }
        public int? DeliveryMaxDays { get; set; }
        public decimal ShippingCost { get; set; }
        public bool InsuranceAvailable { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ShippingCarrier? Carrier { get; set; }
    }

    public class CheckoutSelection
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int PaymentProviderId { get; set; }
        public int ShippingOptionId { get; set; }
        public string AddressSnapshot { get; set; } = "{}"; // store JSON string; mapped to jsonb
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}


