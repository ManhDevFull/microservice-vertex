namespace dotnet.Dtos;

public class WishListItemDTO
{
    public int id { get; set; }
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public string? imageUrl { get; set; }
    public decimal minPrice { get; set; }
    public int totalStock { get; set; }
}

public class WishListAddDTO
{
    public int productId { get; set; }
}
