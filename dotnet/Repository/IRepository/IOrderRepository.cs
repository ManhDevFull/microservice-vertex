using System;
using System.Collections.Generic;
using System.Threading.Tasks;
<<<<<<< HEAD
using System.Threading;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Model;
=======
using dotnet.Dtos;
using dotnet.Dtos.admin;
>>>>>>> user

namespace dotnet.Repository.IRepository
{
  public interface IOrderRepository
  {
    Task<IEnumerable<OrderHistoryDTO>> GetOrderHistoryAsync(int accountId);

<<<<<<< HEAD
    Task<PagedResult<Order>> GetOrdersAsync(
=======
    Task<PagedResult<OrderAdminDTO>> GetOrdersAsync(
>>>>>>> user
      int page,
      int size,
      string? status,
      string? payment,
      string? payType,
      string? keyword,
      DateTime? fromDate,
      DateTime? toDate);

<<<<<<< HEAD
    Task<Order?> GetOrderDetailAsync(int orderId);
    Task<bool> UpdateOrderStatusAsync(int orderId, string status, string? paymentStatus);
    Task<OrderAdminSummaryDTO> GetSummaryAsync();
    Task<CreateOrderResponseDto> CreateOrdersFromCartAsync(
      int accountId,
      CreateOrderRequestDto request,
      CancellationToken cancellationToken = default);
=======
    Task<OrderAdminDTO?> GetOrderDetailAsync(int orderId);
    Task<bool> UpdateOrderStatusAsync(int orderId, string status, string? paymentStatus);
    Task<OrderAdminSummaryDTO> GetSummaryAsync();
>>>>>>> user
  }
}
