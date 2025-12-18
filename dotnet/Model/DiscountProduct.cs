namespace dotnet.Model;
public class DiscountProduct
{
    public int id { get; set; }
    public int discountid { get; set; }
    public int variantid { get; set; }

    public Discount discount { get; set; } = null!;
    public Variant variant { get; set; } = null!;
}