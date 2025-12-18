using System;

namespace dotnet.Dtos.admin
{
  public class OrderAdminQuery
  {
    public int? Page { get; set; }
    public int? Size { get; set; }
    public string? Status { get; set; }
    public string? Payment { get; set; }
    public string? PayType { get; set; }
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
  }
}
