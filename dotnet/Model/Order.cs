namespace dotnet.Model;
<<<<<<< HEAD
public class Order
{
    public int id { get; set; }
    public int accountid { get; set; }
    public int variantid { get; set; }
    public int addressid { get; set; }
    public int quantity { get; set; }
    public DateTime orderdate { get; set; }
    public string? statusorder { get; set; }
    public DateTime? receivedate { get; set; }
    public string? typepay { get; set; }
    public string? statuspay { get; set; }

    public Account account { get; set; } = null!;
    public Variant variant { get; set; } = null!;
    public Address address { get; set; } = null!;
    public Review? review { get; set; }
=======

public class Order
{
  public int id { get; set; }
  public int accountid { get; set; }
  public int addressid { get; set; }
  public DateTime orderdate { get; set; }
  public string? statusorder { get; set; }
  public DateTime? receivedate { get; set; }
  public string? typepay { get; set; }
  public string? statuspay { get; set; }

  public Account? account { get; set; }
  public Address? address { get; set; }
  public ICollection<OrderDetail>? orderdetails { get; set; }
>>>>>>> 337f3c50ea813517e90e1dd2cf24129c526ddc69
}
