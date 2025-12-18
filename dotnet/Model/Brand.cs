namespace dotnet.Model;
public class Brand
{
    public long id { get; set; }
    public string name { get; set; } = null!;
    public ICollection<Product> products { get; set; } = new List<Product>();
}