using System.Text.Json;

namespace dotnet.Model;
public class Variant
{
    public int id { get; set; }
    public int productid { get; set; }
    public JsonDocument valuevariant { get; set; } = JsonDocument.Parse("{}"); 
    public int stock { get; set; }
    public int inputprice { get; set; }
    public int price { get; set; }
    public DateTime createdate { get; set; }
    public DateTime updatedate { get; set; }
    public bool isdeleted { get; set; } = false;

    public Product product { get; set; } = null!;
    public ICollection<DiscountProduct> discountProduct { get; set; } = new List<DiscountProduct>();
    public ICollection<Order> orders { get; set; } = new List<Order>();
    public ICollection<ShoppingCart> carts { get; set; } = new List<ShoppingCart>();
}