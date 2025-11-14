using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Data;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            var sql = @"
                SELECT
                    o.id AS ""OrderId"",
                    o.orderdate AS ""OrderDate"",
                    o.statusorder AS ""StatusOrder"",
                    SUM(
                        od.quantity * (
                            CASE
                                WHEN d.typediscount = 1 THEN ROUND((v.price * (1 - COALESCE(d.discount, 0)::NUMERIC / 100.0))::NUMERIC)
                                WHEN d.typediscount = 2 THEN v.price - COALESCE(d.discount, 0)
                                ELSE v.price
                            END
                        )
                    ) AS ""TotalPriceAfterDiscount""
                FROM orders o
                JOIN orderdetail od ON od.order_id = o.id
                JOIN variant v ON od.variant_id = v.id
                JOIN product p ON v.product_id = p.id
                LEFT JOIN discount_product dp ON v.id = dp.variant_id
                LEFT JOIN discount d ON dp.discount_id = d.id
                    AND o.orderdate BETWEEN d.starttime AND d.endtime
                WHERE o.account_id = @accountId
                GROUP BY o.id, o.orderdate, o.statusorder
                ORDER BY o.id DESC;
            ";

            var accountIdParam = new NpgsqlParameter("@accountId", accountId);

            var orders = await _connect.Database
                                   .SqlQueryRaw<OrderHistoryDTO>(sql, accountIdParam)
                                   .AsNoTracking()
                                   .ToListAsync();

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
              o.orderdetails!.Any(od => EF.Functions.ILike(od.variant!.product.nameproduct ?? "", pattern)) ||
              EF.Functions.ILike(o.statusorder ?? "", pattern) ||
              EF.Functions.ILike(o.statuspay ?? "", pattern));
        }
        else
        {
          query = query.Where(o =>
              EF.Functions.ILike((o.account.firstname ?? "") + " " + (o.account.lastname ?? ""), pattern) ||
              EF.Functions.ILike(o.account.email ?? "", pattern) ||
              o.orderdetails!.Any(od => EF.Functions.ILike(od.variant!.product.nameproduct ?? "", pattern)) ||
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

      var orderIds = orders.Select(o => o.id).ToList();
      var detailLookup = await LoadOrderLineLookupAsync(orderIds);

      var items = orders
        .Select(order =>
        {
          detailLookup.TryGetValue(order.id, out var details);
          IReadOnlyList<dotnet.Model.OrderDetail> normalizedDetails =
            details != null
              ? (IReadOnlyList<dotnet.Model.OrderDetail>)details
              : Array.Empty<dotnet.Model.OrderDetail>();
          return MapToDto(order, normalizedDetails);
        })
        .ToList();

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
          .FirstOrDefaultAsync(o => o.id == orderId);

      if (order == null) return null;

      var lookup = await LoadOrderLineLookupAsync(new[] { orderId });
      lookup.TryGetValue(orderId, out var details);
      IReadOnlyList<dotnet.Model.OrderDetail> normalizedDetails =
        details != null
          ? (IReadOnlyList<dotnet.Model.OrderDetail>)details
          : Array.Empty<dotnet.Model.OrderDetail>();

      return MapToDto(order, normalizedDetails);
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

      summary.Revenue = await (from o in _connect.orders.AsNoTracking()
                               join od in _connect.orderdetails.AsNoTracking() on o.id equals od.order_id
                               join v in _connect.variants.AsNoTracking() on od.variant_id equals v.id
                               where o.statusorder == "DELIVERED"
                               select (long)od.quantity * v.price)
                               .SumAsync();

      return summary;
    }

    private static OrderAdminDTO MapToDto(dotnet.Model.Order order, IReadOnlyList<dotnet.Model.OrderDetail> details)
    {
      var account = order.account;
      var address = order.address;
      var resolvedVariantId = primaryDetail?.variant_id ?? 0;
      var quantity = primaryDetail?.quantity ?? 0;
      if (quantity == 0) quantity = 1;

      var unitPrice = variant?.price ?? 0;
      var totalPrice = unitPrice * quantity;

      var normalizedDetails = details?
        .Where(d => d != null)
        .ToList() ?? new List<dotnet.Model.OrderDetail>();

      var primaryDetail = normalizedDetails.FirstOrDefault();
      var variant = primaryDetail?.variant;
      var product = variant?.product;
      var resolvedVariantId = primaryDetail?.variant_id ?? 0;
      var variantAttributes = ExtractAttributes(variant?.valuevariant);
      var productSnapshot = BuildProductSnapshot(product, resolvedVariantId, variantAttributes);

      if (productSnapshot == null && primaryDetail != null)
      {
        var gallery = product?.imageurls ?? Array.Empty<string>();
        productSnapshot = new ProductSnapshotDTO
        {
          ProductId = product?.id ?? 0,
          Name = product?.nameproduct ?? string.Empty,
          Thumbnail = gallery.FirstOrDefault() ?? string.Empty,
          Gallery = gallery,
          VariantId = resolvedVariantId,
          VariantAttributes = new Dictionary<string, string>(variantAttributes, StringComparer.OrdinalIgnoreCase)
        };
      }

      var items = BuildOrderItems(normalizedDetails);
      var primaryItem = items.FirstOrDefault();

      var quantity = primaryItem?.Quantity ?? primaryDetail?.quantity ?? 0;
      if (quantity == 0) quantity = 1;

      var unitPrice = primaryItem?.UnitPrice ?? variant?.price ?? 0;
      var totalPrice = primaryItem?.TotalPrice ?? unitPrice * quantity;
      var resolvedAttributes = productSnapshot?.VariantAttributes ?? variantAttributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      return new OrderAdminDTO
      {
        Id = order.id,
        AccountId = order.accountid,
        VariantId = productSnapshot?.VariantId ?? resolvedVariantId,
        CustomerName = $"{(account?.firstname ?? "").Trim()} {(account?.lastname ?? "").Trim()}".Trim(),
        CustomerEmail = account?.email ?? string.Empty,
        CustomerPhone = address?.tel ?? string.Empty,
        ShippingAddress = BuildAddress(address),
        ProductName = productSnapshot?.Name ?? product?.nameproduct ?? string.Empty,
        ProductImage = productSnapshot?.Thumbnail ?? product?.imageurls?.FirstOrDefault() ?? string.Empty,
        VariantAttributes = resolvedAttributes,
        Items = items,
        Quantity = quantity,
        UnitPrice = unitPrice,
        TotalPrice = totalPrice,
        StatusOrder = order.statusorder ?? string.Empty,
        StatusPay = order.statuspay ?? string.Empty,
        TypePay = order.typepay ?? string.Empty,
        OrderDate = order.orderdate,
        ReceiveDate = order.receivedate,
        Product = productSnapshot
      };
    }

    private static ProductSnapshotDTO? BuildProductSnapshot(dotnet.Model.Product? product, int variantId, Dictionary<string, string> attributes)
    {
      if (product == null)
      {
        return null;
      }

      var gallery = product.imageurls ?? Array.Empty<string>();

      return new ProductSnapshotDTO
      {
        ProductId = product.id,
        Name = product.nameproduct ?? string.Empty,
        Thumbnail = gallery.FirstOrDefault() ?? string.Empty,
        Gallery = gallery,
        VariantId = variantId,
        VariantAttributes = new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase)
      };
    }

    private static List<OrderAdminItemDTO> BuildOrderItems(IReadOnlyList<dotnet.Model.OrderDetail> details)
    {
      if (details == null || details.Count == 0)
      {
        return new List<OrderAdminItemDTO>();
      }

      var items = new List<OrderAdminItemDTO>(details.Count);

      foreach (var detail in details)
      {
        if (detail == null) continue;

        var quantity = detail.quantity == 0 ? 1 : detail.quantity;
        var price = detail.variant?.price ?? 0;
        var attributes = ExtractAttributes(detail.variant?.valuevariant);
        var snapshot = BuildProductSnapshot(detail.variant?.product, detail.variant_id, attributes);

        if (snapshot == null)
        {
          var gallery = detail.variant?.product?.imageurls ?? Array.Empty<string>();
          snapshot = new ProductSnapshotDTO
          {
            ProductId = detail.variant?.product?.id ?? 0,
            Name = detail.variant?.product?.nameproduct ?? string.Empty,
            Thumbnail = gallery.FirstOrDefault() ?? string.Empty,
            Gallery = gallery,
            VariantId = detail.variant_id,
            VariantAttributes = new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase)
          };
        }

        items.Add(new OrderAdminItemDTO
        {
          Product = snapshot,
          Quantity = quantity,
          UnitPrice = price,
          TotalPrice = price * quantity
        });
      }

      return items;
    }

    private async Task<Dictionary<int, List<dotnet.Model.OrderDetail>>> LoadOrderLineLookupAsync(IEnumerable<int> orderIds)
    {
      var ids = orderIds.Distinct().ToList();
      if (ids.Count == 0) return new Dictionary<int, List<dotnet.Model.OrderDetail>>();

      var details = await _connect.orderdetails
        .AsNoTracking()
        .Where(od => ids.Contains(od.order_id))
        .Include(od => od.variant!)
          .ThenInclude(v => v.product)
        .OrderBy(od => od.id)
        .ToListAsync();

      await EnsureVariantGraphLoadedAsync(details);

      return details
        .GroupBy(od => od.order_id)
        .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task EnsureVariantGraphLoadedAsync(List<dotnet.Model.OrderDetail> details)
    {
      if (details.Count == 0) return;

      var missingVariantIds = details
        .Where(od => od.variant == null)
        .Select(od => od.variant_id)
        .Distinct()
        .ToList();

      if (missingVariantIds.Count > 0)
      {
        var variantLookup = await _connect.variants
          .AsNoTracking()
          .Where(v => missingVariantIds.Contains(v.id))
          .Include(v => v.product)
          .ToDictionaryAsync(v => v.id);

        foreach (var detail in details.Where(od => od.variant == null))
        {
          if (variantLookup.TryGetValue(detail.variant_id, out var variant))
          {
            detail.variant = variant;
          }
        }
      }

      var missingProductIds = details
        .Select(od => od.variant)
        .Where(v => v != null && v.product == null)
        .Select(v => v!.product_id)
        .Distinct()
        .ToList();

      if (missingProductIds.Count == 0)
      {
        return;
      }

      var productLookup = await _connect.products
        .AsNoTracking()
        .Where(p => missingProductIds.Contains(p.id))
        .ToDictionaryAsync(p => p.id);

      foreach (var variant in details.Select(od => od.variant).Where(v => v != null && v.product == null))
      {
        if (variant != null && productLookup.TryGetValue(variant.product_id, out var product))
        {
          variant.product = product;
        }
      }
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