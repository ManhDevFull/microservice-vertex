namespace dotnet.Dtos
{
  public class ProductDTO
  {
   public int product_id { get; set; }
    public string product_name { get; set; } = default!;
    public string product_description { get; set; } = default!;
    public string? brand { get; set; }
    public int category_id { get; set; }
    public string category_name { get; set; } = default!;
    public string[] imageurls { get; set; } = Array.Empty<string>();
    public DateTime product_created { get; set; }
    public DateTime product_updated { get; set; }
    public long total_stock { get; set; }
    public List<ValueVariant> variants { get; set; } = new();

    public class ValueVariant
    {
      public int id { get; set; }
      public string value { get; set; } = default!;
      public int stock { get; set; }
      public int inputPrice { get; set; }
      public int price { get; set; }
      public DateTime createDate { get; set; }
      public DateTime updateDate { get; set; }
    }
  }
}
