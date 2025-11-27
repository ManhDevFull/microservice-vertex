using System.Collections.Generic;

namespace dotnet.Dtos;

public class CreateOrderRequestDto
{
    public string OrderId { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public int? AddressId { get; set; }
    public CustomerInfoDto? CustomerInfo { get; set; }
    public string? PaymentMethod { get; set; }
    /// <summary>
    /// Optional flag that controls whether the shopping cart should be cleared
    /// after the order is created. Defaults to true to preserve current behaviour.
    /// </summary>
    public bool? ClearCart { get; set; }
}

public class CustomerInfoDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Country { get; set; } = "Vietnam";
    public string State { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class CreateOrderResponseDto
{
    public bool Success { get; set; }
    public string OrderToken { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public int AddressId { get; set; }
    public int Items { get; set; }
    public string StatusOrder { get; set; } = string.Empty;
    public string StatusPay { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public bool CartCleared { get; set; }
    public IReadOnlyList<int> OrderDetailIds { get; set; } = new List<int>();
}

