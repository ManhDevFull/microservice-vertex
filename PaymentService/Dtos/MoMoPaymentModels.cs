namespace PaymentService.Dtos;

public class MoMoPaymentResult
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; }
    public string? Message { get; set; }
}

public class MoMoApiResponse
{
    public int resultCode { get; set; }
    public string? message { get; set; }
    public string? payUrl { get; set; }
    public string? qrCode { get; set; }
    public string? deeplink { get; set; }
}

