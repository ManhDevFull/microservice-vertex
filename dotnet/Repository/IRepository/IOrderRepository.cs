using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Model;
using dotnet.Dtos.track_order;

namespace dotnet.Repository.IRepository
{
  public interface IOrderRepository
  {
    Task <ICollection<TrackOrderDTO>> getTrackOrder(int idAccount);
    Task <TimeLineDTO>getTimeLineByIdOrder(int idOrder);
    Task<IEnumerable<OrderHistoryDTO>> GetOrderHistoryAsync(int accountId);

    Task<PagedResult<Order>> GetOrdersAsync(
      int page,
      int size,
      string? status,
      string? payment,
      string? payType,
      string? keyword,
      DateTime? fromDate,
      DateTime? toDate);

    Task<Order?> GetOrderDetailAsync(int orderId);
    Task<bool> UpdateOrderStatusAsync(int orderId, string status, string? paymentStatus);
    Task<OrderAdminSummaryDTO> GetSummaryAsync();
    Task<CreateOrderResponseDto> CreateOrdersFromCartAsync(
      int accountId,
      CreateOrderRequestDto request,
      CancellationToken cancellationToken = default);
  }
}
