namespace dotnet.Dtos.admin
{
  public class OrderAdminUpdateStatusRequest
  {
    public string Status { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
  }
}
