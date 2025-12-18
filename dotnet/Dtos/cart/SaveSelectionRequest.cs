namespace dotnet.Dtos.Cart;

public class SaveSelectionRequest
{
    public int PaymentProviderId { get; set; }
    public int ShippingOptionId { get; set; }
    public object AddressSnapshot { get; set; } = new();
}

