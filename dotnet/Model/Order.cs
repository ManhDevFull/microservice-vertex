namespace dotnet.Model;

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
  public Review? review { get; set; }
}