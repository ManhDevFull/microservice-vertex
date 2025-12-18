namespace dotnet.Model;
public class ShoppingCart
{
    public int id { get; set; }
    public int accountid { get; set; }
    public int variantid { get; set; }
    public int quantity { get; set; }

    public Account account { get; set; } = null!;
    public Variant variant { get; set; } = null!;
}
