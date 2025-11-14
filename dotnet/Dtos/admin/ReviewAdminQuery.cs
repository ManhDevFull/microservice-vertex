using System;

namespace dotnet.Dtos.admin
{
  public class ReviewAdminQuery
  {
    public int? Page { get; set; }
    public int? Size { get; set; }
    public int? Rating { get; set; }
    public bool? Updated { get; set; }
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
  }
}
