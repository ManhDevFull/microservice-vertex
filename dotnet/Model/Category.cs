using dotnet.Model;

namespace be_dotnet_ecommerce1.Model
{
  public class Category
  {
    public int id { get; set; }
    public string namecategory { get; set; } = null!;
    public int? idparent { get; set; }
    public List<Product>? Products { get; set; }
  }
}