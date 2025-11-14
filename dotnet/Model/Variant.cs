using System.Text.Json;

namespace dotnet.Model;
public class Variant
{
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