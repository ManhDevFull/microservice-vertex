using Microsoft.EntityFrameworkCore;
using be_dotnet_ecommerce1.Data;
using dotnet.Dtos;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using dotnet.Dtos.admin;
using dotnet.Repository.IRepository;

// (Đảm bảo đã thêm 2 using này)
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ConnectData _connect;
        public OrderRepository(ConnectData connect)
        {
            _connect = connect;
        }

        public async Task<IEnumerable<OrderHistoryDTO>> GetOrderHistoryAsync(int accountId)
        {
            // Câu SQL của bạn (giữ nguyên)
            var sql = @"
                SELECT
                    o.id AS ""OrderId"",
                    o.orderdate AS ""OrderDate"",
                    o.statusorder AS ""StatusOrder"",
                    o.quantity * (
                        CASE
                            WHEN d.typediscount = 1 THEN ROUND((v.price * (1 - COALESCE(d.discount, 0)::NUMERIC / 100.0))::NUMERIC)
                            WHEN d.typediscount = 2 THEN v.price - COALESCE(d.discount, 0)
                            ELSE v.price
                        END
                    ) AS ""TotalPriceAfterDiscount""
                FROM orders o
                JOIN variant v ON o.variant_id = v.id
                JOIN product p ON v.product_id = p.id
                LEFT JOIN discount_product dp ON v.id = dp.variant_id
                LEFT JOIN discount d ON dp.discount_id = d.id
                    AND o.orderdate BETWEEN d.starttime AND d.endtime
                WHERE o.account_id = @accountId
                ORDER BY o.id DESC;
            ";

            var accountIdParam = new NpgsqlParameter("@accountId", accountId);

            // === SỬA LẠI DÒNG NÀY ===
            // Thay _connect.Set<OrderHistoryDTO>().FromSqlRaw(...)
            var orders = await _connect.Database
                                   .SqlQueryRaw<OrderHistoryDTO>(sql, accountIdParam) // Dùng SqlQueryRaw
                                   .AsNoTracking()
                                   .ToListAsync();
            // ======================
            foreach (var o in orders)
            {
                Console.WriteLine($"ID={o.OrderId}, Price={o.TotalPriceAfterDiscount}");
            }


            return orders;
        }


    public async Task<PagedResult<OrderAdminDTO>> GetOrdersAsync(
        int page,
        int size,
        string? status,
        string? payment,
        string? payType,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
      page = Math.Max(1, page);
      size = Math.Clamp(size, 1, 100);
      var offset = (page - 1) * size;

      var query = _connect.orders
          .AsNoTracking()
          .Include(o => o.account)
          .Include(o => o.address)
          .Include(o => o.variant)
            .ThenInclude(v => v.product)
          .AsQueryable();

      if (!string.IsNullOrWhiteSpace(status))
      {
        var normalizedStatus = status.Trim().ToUpperInvariant();
        query = query.Where(o => o.statusorder != null && o.statusorder.ToUpper() == normalizedStatus);
      }

      if (!string.IsNullOrWhiteSpace(payment))
      {
        var normalizedPayment = payment.Trim().ToUpperInvariant();
        query = query.Where(o => o.statuspay != null && o.statuspay.ToUpper() == normalizedPayment);
      }

      if (!string.IsNullOrWhiteSpace(payType))
      {
        var normalizedPayType = payType.Trim().ToUpperInvariant();
        query = query.Where(o => o.typepay != null && o.typepay.ToUpper() == normalizedPayType);
      }

      if (fromDate.HasValue)
      {
        var from = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
        query = query.Where(r => r.orderdate >= from);
      }

      if (toDate.HasValue)
      {
        var to = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
        query = query.Where(r => r.orderdate < to);
      }

      if (!string.IsNullOrWhiteSpace(keyword))
      {
        var trimmed = keyword.Trim();
        var pattern = $"%{trimmed}%";

        if (int.TryParse(trimmed, out var orderId))
        {
          query = query.Where(o =>
              o.id == orderId ||
              EF.Functions.ILike((o.account.firstname ?? "") + " " + (o.account.lastname ?? ""), pattern) ||
              EF.Functions.ILike(o.account.email ?? "", pattern) ||
              EF.Functions.ILike(o.variant.product.nameproduct ?? "", pattern) ||
              EF.Functions.ILike(o.statusorder ?? "", pattern) ||
              EF.Functions.ILike(o.statuspay ?? "", pattern));
        }
        else
        {
          query = query.Where(o =>
              EF.Functions.ILike((o.account.firstname ?? "") + " " + (o.account.lastname ?? ""), pattern) ||
              EF.Functions.ILike(o.account.email ?? "", pattern) ||
              EF.Functions.ILike(o.variant.product.nameproduct ?? "", pattern) ||
              EF.Functions.ILike(o.statusorder ?? "", pattern) ||
              EF.Functions.ILike(o.statuspay ?? "", pattern));
        }
      }

      var total = await query.CountAsync();

      var orders = await query
          .OrderByDescending(o => o.orderdate)
          .Skip(offset)
          .Take(size)
          .ToListAsync();

      var items = orders.Select(MapToDto).ToList();

      return new PagedResult<OrderAdminDTO>
      {
        Items = items,
        Total = total,
        Page = page,
        Size = size
      };
    }

    public async Task<OrderAdminDTO?> GetOrderDetailAsync(int orderId)
    {
      var order = await _connect.orders
          .AsNoTracking()
          .Include(o => o.account)
          .Include(o => o.address)
          .Include(o => o.variant)
            .ThenInclude(v => v.product)
          .FirstOrDefaultAsync(o => o.id == orderId);

      return order == null ? null : MapToDto(order);
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status, string? paymentStatus)
    {
      var order = await _connect.orders.FirstOrDefaultAsync(o => o.id == orderId);
      if (order == null)
      {
        return false;
      }

      order.statusorder = status.Trim().ToUpperInvariant();

      if (!string.IsNullOrWhiteSpace(paymentStatus))
      {
        order.statuspay = paymentStatus.Trim().ToUpperInvariant();
      }

      if (order.statusorder == "DELIVERED")
      {
        order.receivedate = DateTime.UtcNow;
      }

      if (order.statusorder == "CANCELLED")
      {
        order.receivedate = null;
      }

      await _connect.SaveChangesAsync();
      return true;
    }

    public async Task<OrderAdminSummaryDTO> GetSummaryAsync()
    {
      var summary = new OrderAdminSummaryDTO();

      var statusCounts = await _connect.orders
          .AsNoTracking()
          .GroupBy(o => o.statusorder ?? "UNKNOWN")
          .Select(g => new { Status = g.Key, Count = g.Count() })
          .ToListAsync();

      foreach (var item in statusCounts)
      {
        switch (item.Status.ToUpperInvariant())
        {
          case "PENDING":
            summary.Pending = item.Count;
            break;
          case "SHIPPED":
            summary.Shipped = item.Count;
            break;
          case "DELIVERED":
            summary.Delivered = item.Count;
            break;
          case "CANCELLED":
            summary.Cancelled = item.Count;
            break;
        }
        summary.Total += item.Count;
      }

      var paymentCounts = await _connect.orders
          .AsNoTracking()
          .GroupBy(o => o.statuspay ?? "UNKNOWN")
          .Select(g => new { Status = g.Key, Count = g.Count() })
          .ToListAsync();

      foreach (var item in paymentCounts)
      {
        switch (item.Status.ToUpperInvariant())
        {
          case "PAID":
            summary.Paid = item.Count;
            break;
          case "UNPAID":
            summary.Unpaid = item.Count;
            break;
        }
      }

      var deliveredRevenue = await (from o in _connect.orders.AsNoTracking()
                                    join v in _connect.variants.AsNoTracking()
                                      on o.variantid equals v.id
                                    where o.statusorder == "DELIVERED"
                                    select new { o.quantity, v.price })
                                    .ToListAsync();

      summary.Revenue = deliveredRevenue.Sum(item => (long)item.quantity * item.price);

      return summary;
    }

    private static OrderAdminDTO MapToDto(dotnet.Model.Order order)
    {
      var variant = order.variant;
      var product = variant?.product;
      var account = order.account;
      var address = order.address;

      return new OrderAdminDTO
      {
        Id = order.id,
        AccountId = order.accountid,
        VariantId = order.variantid,
        CustomerName = $"{(account?.firstname ?? "").Trim()} {(account?.lastname ?? "").Trim()}".Trim(),
        CustomerEmail = account?.email ?? string.Empty,
        CustomerPhone = address?.tel ?? string.Empty,
        ShippingAddress = BuildAddress(address),
        ProductName = product?.nameproduct ?? string.Empty,
        ProductImage = product?.imageurls?.FirstOrDefault() ?? string.Empty,
        VariantAttributes = ExtractAttributes(variant?.valuevariant),
        Quantity = order.quantity,
        UnitPrice = variant?.price ?? 0,
        TotalPrice = (variant?.price ?? 0) * order.quantity,
        StatusOrder = order.statusorder ?? string.Empty,
        StatusPay = order.statuspay ?? string.Empty,
        TypePay = order.typepay ?? string.Empty,
        OrderDate = order.orderdate,
        ReceiveDate = order.receivedate
      };
    }

    private static string BuildAddress(dotnet.Model.Address? address)
    {
      if (address == null) return string.Empty;

      var parts = new List<string>();

      if (!string.IsNullOrWhiteSpace(address.detail))
      {
        parts.Add(address.detail.Trim());
      }

      if (!string.IsNullOrWhiteSpace(address.description))
      {
        parts.Add(address.description.Trim());
      }

      return string.Join(", ", parts);
    }

    private static Dictionary<string, string> ExtractAttributes(JsonDocument? document)
    {
      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      if (document == null)
      {
        return result;
      }

      var root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        return result;
      }

      foreach (var property in root.EnumerateObject())
      {
        result[property.Name] = property.Value.ValueKind switch
        {
          JsonValueKind.String => property.Value.GetString() ?? string.Empty,
          JsonValueKind.Number => property.Value.TryGetInt64(out var longVal)
              ? longVal.ToString()
              : property.Value.TryGetDecimal(out var decVal)
                ? decVal.ToString("G")
                : property.Value.ToString(),
          JsonValueKind.True => "true",
          JsonValueKind.False => "false",
          _ => property.Value.ToString()
        };
      }

      return result;
    }
  }
}
