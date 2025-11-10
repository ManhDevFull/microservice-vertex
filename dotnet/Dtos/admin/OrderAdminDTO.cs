using System;
using System.Collections.Generic;

namespace dotnet.Dtos.admin
{
  public class OrderAdminDTO
  {
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int VariantId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public Dictionary<string, string> VariantAttributes { get; set; } = new();
    public int Quantity { get; set; }
    public int UnitPrice { get; set; }
    public int TotalPrice { get; set; }
    public string StatusOrder { get; set; } = string.Empty;
    public string StatusPay { get; set; } = string.Empty;
    public string TypePay { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ReceiveDate { get; set; }
  }
}
