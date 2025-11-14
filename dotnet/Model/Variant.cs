using System.Text.Json;

namespace dotnet.Model;
public class Variant
{
<<<<<<< HEAD
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
=======
  public int id { get; set; }
  public int product_id { get; set; }
  public JsonDocument? valuevariant { get; set; }
  public int stock { get; set; }
  public int inputprice { get; set; }
  public int price { get; set; }
  public DateTime createdate { get; set; }
  public DateTime updatedate { get; set; }
  public bool isdeleted { get; set; } = false;

  public Product? product { get; set; }
  public ICollection<OrderDetail>? orderdetails { get; set; }
  public ICollection<ShoppingCart>? carts { get; set; }
  public ICollection<DiscountProduct>? discountProduct { get; set; }
}
>>>>>>> 337f3c50ea813517e90e1dd2cf24129c526ddc69
