namespace dotnet.Dtos.admin
{
  public class VariantAdminUpdateRequest
  {
    public Dictionary<string, string>? valuevariant { get; set; }
    public int? stock { get; set; }
    public int? inputprice { get; set; }
    public int? price { get; set; }
  }

  public class ProductAdminUpdateRequest
  {
    public string? name { get; set; }
    public long? brandId { get; set; }
    public int? categoryId { get; set; }
    public string? description { get; set; }
    public List<string>? imageUrls { get; set; }
  }
}
