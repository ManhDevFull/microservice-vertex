using System.Text.Json;

namespace dotnet.Dtos;

public class CartItemDto
{
    public int cartId { get; set; }
    public int variantId { get; set; }
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public string? imageUrl { get; set; }
    public string? color { get; set; }
    public int unitPrice { get; set; }
    public int quantity { get; set; }

    public static string? ExtractColor(string? valueVariantJson)
    {
        if (string.IsNullOrWhiteSpace(valueVariantJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(valueVariantJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("color", out var colorEl))
            {
                return colorEl.GetString();
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("Color", out var colorEl2))
            {
                return colorEl2.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}

public class CartSummaryDto
{
    public decimal itemsPrice { get; set; }
    public decimal shipping { get; set; }
    public decimal tax { get; set; }
    public decimal discountPrice { get; set; }
    public decimal giftBoxPrice { get; set; } = 10.90m;
    public decimal totalPrice { get; set; }
}

public class CartResponseDto
{
    public List<CartItemDto> items { get; set; } = new();
    public CartSummaryDto summary { get; set; } = new();
}

public class CartAddDto
{
    public int variantId { get; set; }
    public int quantity { get; set; } = 1;
}

public class CartUpdateQtyDto
{
    public int cartId { get; set; }
    public int quantity { get; set; }
}

