namespace dotnet.Model
{
  public class Product
  {
    public int id { get; set; }
    public string nameproduct { get; set; } = null!;
    public long brand_id { get; set; }                    // FK
    public string description { get; set; } = null!;
    public int categoryId { get; set; }
    public string[] imageurls { get; set; } = Array.Empty<string>();
    public DateTime createdate { get; set; }
    public DateTime updatedate { get; set; }
    public bool isdeleted { get; set; }

    public Brand brand { get; set; } = null!;            // <— navigation
    public Category category { get; set; } = null!;
    public ICollection<Variant> variants { get; set; } = new List<Variant>();
       public ICollection<WishList> wishLists { get; set; } = new List<WishList>();
  }
}