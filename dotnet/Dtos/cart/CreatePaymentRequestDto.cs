namespace dotnet.Dtos.Cart;

public class CreatePaymentRequestDto
{
    public string? OrderId { get; set; }
    public long Amount { get; set; }
    public string? OrderInfo { get; set; }
    public string? ReturnUrl { get; set; }
    public List<int>? SelectedCartIds { get; set; }
    public int? AddressId { get; set; }
}

