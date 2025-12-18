namespace dotnet.Dtos;

public class CartSuggestionDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public string? imageUrl { get; set; }
    public decimal minPrice { get; set; }
    public decimal? oldPrice { get; set; }
    public int? discountPercent { get; set; }
    public int? fixedDiscount { get; set; }
    public string categoryName { get; set; } = string.Empty;
}

