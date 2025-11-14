using System.Collections.Generic;

namespace dotnet.Dtos.admin
{
  public class OrderAdminItemDTO
  {
    public ProductSnapshotDTO Product { get; set; } = new();
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public int TotalPrice { get; set; }
  }
}

