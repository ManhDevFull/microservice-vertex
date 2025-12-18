namespace PaymentService.Dtos
{
    public class CreatePaymentRequestDto
    {
        public long Amount { get; set; }
        public string? OrderInfo { get; set; }
        public string? ReturnUrl { get; set; }
        public string? OrderId { get; set; }
        public int? AccountId { get; set; }
        public List<int>? SelectedCartIds { get; set; }
        public int? AddressId { get; set; }
    }
}
