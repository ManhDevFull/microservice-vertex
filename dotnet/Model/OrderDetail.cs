namespace dotnet.Model
{
  public class OrderDetail
  {
    public int id { get; set; }
    public int order_id { get; set; }
    public int variant_id { get; set; }
    public int quantity { get; set; }

    public Order? order { get; set; }
    public Variant? variant { get; set; }
    public ICollection<Review>? reviews { get; set; }

  }
}