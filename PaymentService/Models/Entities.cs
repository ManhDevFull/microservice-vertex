namespace PaymentService.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public string OrderId { get; set; } = default!; // Unique order ID from main backend
        public int AccountId { get; set; } // User ID
        public string PartnerCode { get; set; } = default!; // MoMo partner code
        public string RequestId { get; set; } = default!; // MoMo request ID
        public long Amount { get; set; } // Amount in VND (MoMo uses long)
        public string OrderInfo { get; set; } = default!; // Order description
        public string? PaymentUrl { get; set; } // MoMo payment URL (for redirect)
        public string? QrCode { get; set; } // QR code data (if available)
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED, CANCELLED
        public string? MoMoTransactionId { get; set; } // MoMo transaction ID (after payment)
        public string? ResponseCode { get; set; } // MoMo response code
        public string? Message { get; set; } // MoMo response message
        public string? Signature { get; set; } // MoMo signature for verification
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; } // When payment was completed
    }
}

