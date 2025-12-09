using System.Collections.Generic;

namespace dotnet.Dtos
{
  public class OrderDetailResponseDto
  {
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string StatusOrder { get; set; } = string.Empty;
    public string TypePay { get; set; } = string.Empty;
    public string StatusPay { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalPriceAfterDiscount { get; set; }
    public OrderAddressInfoDto AddressInfo { get; set; } = new OrderAddressInfoDto();
    public List<OrderDetailItemDto> Items { get; set; } = new List<OrderDetailItemDto>();
  }

  public class OrderAddressInfoDto
  {
    public string Title { get; set; } = string.Empty;
    public string NameRecipient { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
  }

  public class OrderDetailItemDto
  {
    public int Id { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderProductInfoDto Product { get; set; } = new OrderProductInfoDto();
    public bool CanReview { get; set; }
  }

  public class OrderProductInfoDto
  {
    public string Name { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public Dictionary<string, string> VariantAttributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
  }
}
