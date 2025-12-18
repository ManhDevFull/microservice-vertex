using System.Collections.Generic;

namespace dotnet.Dtos.admin
{
  public class OrderAdminItemDTO
  {
    public int Id { get; set; }
    public ProductSnapshotDTO Product { get; set; } = new();
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public int TotalPrice { get; set; }
  }
}

