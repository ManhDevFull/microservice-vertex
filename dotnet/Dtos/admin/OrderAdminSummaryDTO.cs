namespace dotnet.Dtos.admin
{
  public class OrderAdminSummaryDTO
  {
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Shipped { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public int Paid { get; set; }
    public int Unpaid { get; set; }
    public long Revenue { get; set; }
  }
}
