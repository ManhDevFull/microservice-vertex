using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Dtos.admin;

namespace dotnet.Repository.IRepository
{
  public interface IOrderRepository
  {
    Task<IEnumerable<OrderHistoryDTO>> GetOrderHistoryAsync(int accountId);

    Task<PagedResult<OrderAdminDTO>> GetOrdersAsync(
      int page,
      int size,
      string? status,
      string? payment,
      string? payType,
      string? keyword,
      DateTime? fromDate,
      DateTime? toDate);

    Task<OrderAdminDTO?> GetOrderDetailAsync(int orderId);
    Task<bool> UpdateOrderStatusAsync(int orderId, string status, string? paymentStatus);
    Task<OrderAdminSummaryDTO> GetSummaryAsync();
  }
}
