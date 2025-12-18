namespace dotnet.Model;
public class Category
{
    public int id { get; set; }
    public string namecategory { get; set; } = null!;
    public int? idparent { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}