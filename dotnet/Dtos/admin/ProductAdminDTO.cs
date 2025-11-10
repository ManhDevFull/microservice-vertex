using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace dotnet.Dtos.admin
{
  [Keyless]
  public sealed class ProductAdminDTO
  {
    public int product_id { get; set; }
    public string name { get; set; } = "";
    public string? brand { get; set; }
    public string description { get; set; } = "";
    public int category_id { get; set; }
    public string? category_name { get; set; }
    public string[] imageurls { get; set; } = Array.Empty<string>();
    public DateTime? createdate { get; set; }
    public DateTime? updatedate { get; set; }
    public JsonDocument variants { get; set; } = JsonDocument.Parse("[]");
    public int variant_count { get; set; }
    public int? min_price { get; set; }
    public int? max_price { get; set; }
  }
}
