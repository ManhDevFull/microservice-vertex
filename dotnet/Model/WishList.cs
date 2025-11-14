namespace dotnet.Model;
public class WishList
{
    public int id { get; set; }
    public int accountid { get; set; }
    public int productid { get; set; }

    public Account account { get; set; } = null!;
    public Product product { get; set; } = null!;
}