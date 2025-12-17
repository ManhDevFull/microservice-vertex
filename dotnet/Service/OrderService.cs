using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Dtos.track_order;
using dotnet.Model;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;

namespace dotnet.Service
{
  public class OrderService : IOrderService
  {
    private readonly IOrderRepository _repo;
    public OrderService(IOrderRepository repo)
    {
      _repo = repo;
    }

    public async Task<IEnumerable<OrderHistoryDTO>> GetOrderHistoryAsync(int accountId)
    {
      return await _repo.GetOrderHistoryAsync(accountId);
    }

    public Task<PagedResult<Order>> GetOrdersAsync(
        int page,
        int size,
        string? status,
        string? payment,
        string? payType,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
      return _repo.GetOrdersAsync(page, size, status, payment, payType, keyword, fromDate, toDate);
    }

    public Task<Order?> GetOrderDetailAsync(int orderId)
    {
      return _repo.GetOrderDetailAsync(orderId);
    }

    public Task<bool> UpdateOrderStatusAsync(int orderId, string status, string? paymentStatus)
    {
      return _repo.UpdateOrderStatusAsync(orderId, status, paymentStatus);
    }

    public Task<OrderAdminSummaryDTO> GetSummaryAsync()
    {
      return _repo.GetSummaryAsync();
    }

    public Task<CreateOrderResponseDto> CreateOrdersFromCartAsync(
      int accountId,
      CreateOrderRequestDto request,
      CancellationToken cancellationToken = default)
    {
      return _repo.CreateOrdersFromCartAsync(accountId, request, cancellationToken);
    }

    public async Task<ICollection<TrackOrderDTO>> getTrackOrder(int id)
    {
      var rs = await _repo.getTrackOrder(id);
      return rs;
    }

    public Task<TimeLineDTO> getTimeLineByIdOrder(int idOrder)
    {
      var rs = _repo.getTimeLineByIdOrder(idOrder);
      return rs;
    }

  }
}
