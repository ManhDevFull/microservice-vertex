namespace dotnet.Model
{
  using System.ComponentModel.DataAnnotations.Schema;

  public class OrderDetail
  {
    public int id { get; set; }
    public int order_id { get; set; }
    public int variant_id { get; set; }
    public int quantity { get; set; }

    public Order? order { get; set; }
    public Variant? variant { get; set; }
    public ICollection<Review>? reviews { get; set; }

    [NotMapped]
    public bool canReview { get; set; }

  }
}
