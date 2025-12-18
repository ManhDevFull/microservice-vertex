using System.Text.Json.Serialization;

namespace dotnet.Dtos.admin
{
  public class VariantAdminCreateRequest
  {
    public Dictionary<string, string> valuevariant { get; set; } = new();
    public int stock { get; set; }
    public int inputprice { get; set; }
    public int price { get; set; }
  }

  public class ProductAdminCreateRequest
  {
    public string name { get; set; } = null!;
    public long brandId { get; set; }
    public int categoryId { get; set; }
    public string description { get; set; } = null!;
    public List<string> imageUrls { get; set; } = new();
    public List<VariantAdminCreateRequest> variants { get; set; } = new();
  }
}
