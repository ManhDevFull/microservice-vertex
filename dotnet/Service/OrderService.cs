using System;
using System.Collections.Generic;
<<<<<<< HEAD
using System.Threading;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Model;
=======
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Dtos.admin;
>>>>>>> user
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

<<<<<<< HEAD
    public Task<PagedResult<Order>> GetOrdersAsync(
=======
    public Task<PagedResult<OrderAdminDTO>> GetOrdersAsync(
>>>>>>> user
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

<<<<<<< HEAD
    public Task<Order?> GetOrderDetailAsync(int orderId)
=======
    public Task<OrderAdminDTO?> GetOrderDetailAsync(int orderId)
>>>>>>> user
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
<<<<<<< HEAD

    public Task<CreateOrderResponseDto> CreateOrdersFromCartAsync(
      int accountId,
      CreateOrderRequestDto request,
      CancellationToken cancellationToken = default)
    {
      return _repo.CreateOrdersFromCartAsync(accountId, request, cancellationToken);
    }
=======
>>>>>>> user
  }
}
