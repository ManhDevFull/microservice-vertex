// Dtos/OrderHistoryDTO.cs
namespace dotnet.Dtos
{
public class OrderHistoryDTO 
{
    public int OrderId { get; set; } // Phải khớp với AS OrderId
    public DateTime OrderDate { get; set; } // Phải khớp với AS OrderDate
    public string StatusOrder { get; set; } = string.Empty; // Phải khớp với AS StatusOrder
    public decimal TotalPriceAfterDiscount { get; set; } // Phải khớp với AS TotalPriceAfterDiscount
}
}
