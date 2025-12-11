using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Data;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Dtos.track_order;
using dotnet.Model;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace dotnet.Repository
{
  public class OrderRepository : IOrderRepository
  {
    private readonly ConnectData _connect;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(ConnectData connect, ILogger<OrderRepository> logger)
    {
      _connect = connect;
      _logger = logger;
    }

    public async Task<CreateOrderResponseDto> CreateOrdersFromCartAsync(
      int accountId,
      CreateOrderRequestDto request,
      CancellationToken cancellationToken = default)
    {
      if (request == null)
      {
        throw new ArgumentNullException(nameof(request));
      }

      if (string.IsNullOrWhiteSpace(request.OrderId))
      {
        throw new InvalidOperationException("OrderId is required.");
      }

      _logger.LogInformation("Creating order from cart for account {AccountId}, OrderId: {OrderId}", accountId, request.OrderId);

      var cartQuery = _connect.shoppingCarts
          .Where(sc => sc.accountid == accountId);

      if (request.SelectedCartIds != null && request.SelectedCartIds.Count > 0)
      {
        var ids = request.SelectedCartIds.Distinct().ToList();
        cartQuery = cartQuery.Where(sc => ids.Contains(sc.id));
      }

      var cartItems = await cartQuery.ToListAsync(cancellationToken);

      _logger.LogInformation("Found {Count} cart items for account {AccountId}. SelectedCartIds: {SelectedCartIds}",
          cartItems.Count, accountId,
          request.SelectedCartIds != null && request.SelectedCartIds.Count > 0
              ? string.Join(",", request.SelectedCartIds)
              : "all items");

      if (cartItems.Count == 0)
      {
        _logger.LogWarning("Cart is empty for account {AccountId}. SelectedCartIds: {SelectedCartIds}",
            accountId,
            request.SelectedCartIds != null && request.SelectedCartIds.Count > 0
                ? string.Join(",", request.SelectedCartIds)
                : "all items");
        throw new InvalidOperationException($"Cart is empty for account {accountId}.");
      }

      await using var transaction = await _connect.Database.BeginTransactionAsync(cancellationToken);

      int addressId;
      Order order;
      List<OrderDetail> orderDetails;
      bool clearCart;

      try
      {
        _logger.LogInformation("Resolving address for account {AccountId}", accountId);
        addressId = await ResolveAddressAsync(accountId, request, cancellationToken);
        _logger.LogInformation("Address resolved: {AddressId}", addressId);

        order = new Order
        {
          accountid = accountId,
          addressid = addressId,
          orderdate = DateTime.UtcNow,
          statusorder = "PENDING",
          typepay = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "MOMO" : request.PaymentMethod!.Trim(),
          statuspay = "PAID"
        };

        _connect.orders.Add(order);
        await _connect.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Order created with ID: {OrderId}", order.id);

        orderDetails = cartItems.Select(item => new OrderDetail
        {
          order_id = order.id,
          variant_id = item.variantid,
          quantity = Math.Max(1, item.quantity)
        }).ToList();

        _connect.orderdetails.AddRange(orderDetails);
        await _connect.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created {Count} order details", orderDetails.Count);

        clearCart = request.ClearCart ?? true;
        if (clearCart)
        {
          _logger.LogInformation("Clearing {Count} cart items for account {AccountId}", cartItems.Count, accountId);
          _connect.shoppingCarts.RemoveRange(cartItems);
          await _connect.SaveChangesAsync(cancellationToken);
          _logger.LogInformation("Cart cleared successfully");
        }
        else
        {
          _logger.LogInformation("Cart clearing skipped (ClearCart=false)");
        }

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Transaction committed successfully for order {OrderId}", order.id);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating order: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
        await transaction.RollbackAsync(cancellationToken);
        _logger.LogWarning("Transaction rolled back due to error");
        throw;
      }

      return new CreateOrderResponseDto
      {
        Success = true,
        OrderToken = request.OrderId,
        OrderId = order.id,
        AddressId = addressId,
        Items = orderDetails.Count,
        PaymentMethod = order.typepay ?? string.Empty,
        StatusOrder = order.statusorder ?? string.Empty,
        StatusPay = order.statuspay ?? string.Empty,
        CartCleared = clearCart,
        OrderDetailIds = orderDetails.Select(od => od.id).ToList()
      };
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


    public async Task<PagedResult<Order>> GetOrdersAsync(
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
              EF.Functions.ILike(
                ((o.account != null ? (o.account.firstname ?? string.Empty) + " " + (o.account.lastname ?? string.Empty) : string.Empty)).Trim(),
                pattern) ||
              EF.Functions.ILike(o.account != null ? (o.account.email ?? string.Empty) : string.Empty, pattern) ||
              (o.orderdetails != null && o.orderdetails.Any(od =>
                od.variant != null &&
                od.variant.product != null &&
                EF.Functions.ILike(od.variant.product.nameproduct ?? string.Empty, pattern))) ||
              EF.Functions.ILike(o.statusorder ?? string.Empty, pattern) ||
              EF.Functions.ILike(o.statuspay ?? string.Empty, pattern));
        }
        else
        {
          query = query.Where(o =>
              EF.Functions.ILike(
                ((o.account != null ? (o.account.firstname ?? string.Empty) + " " + (o.account.lastname ?? string.Empty) : string.Empty)).Trim(),
                pattern) ||
              EF.Functions.ILike(o.account != null ? (o.account.email ?? string.Empty) : string.Empty, pattern) ||
              (o.orderdetails != null && o.orderdetails.Any(od =>
                od.variant != null &&
                od.variant.product != null &&
                EF.Functions.ILike(od.variant.product.nameproduct ?? string.Empty, pattern))) ||
              EF.Functions.ILike(o.statusorder ?? string.Empty, pattern) ||
              EF.Functions.ILike(o.statuspay ?? string.Empty, pattern));
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
          IReadOnlyList<OrderDetail> normalizedDetails =
            details != null
              ? (IReadOnlyList<OrderDetail>)details
              : Array.Empty<OrderDetail>();
          return SanitizeOrder(order, normalizedDetails);
        })
        .ToList();

      return new PagedResult<Order>
      {
        Items = items,
        Total = total,
        Page = page,
        Size = size
      };
    }

    public async Task<Order?> GetOrderDetailAsync(int orderId)
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

      return SanitizeOrder(order, normalizedDetails);
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

    private static Order SanitizeOrder(Order order, IReadOnlyList<OrderDetail> details)
    {
      var safeDetails = details?
        .Where(d => d != null)
        .Select(SanitizeOrderDetail)
        .ToList() ?? new List<OrderDetail>();

      return new Order
      {
        id = order.id,
        accountid = order.accountid,
        addressid = order.addressid,
        orderdate = order.orderdate,
        statusorder = order.statusorder ?? string.Empty,
        receivedate = order.receivedate,
        typepay = order.typepay ?? string.Empty,
        statuspay = order.statuspay ?? string.Empty,
        account = SanitizeAccount(order.account),
        address = SanitizeAddress(order.address),
        orderdetails = safeDetails
      };
    }

    private static OrderDetail SanitizeOrderDetail(OrderDetail detail)
    {
      if (detail == null)
      {
        return new OrderDetail
        {
          quantity = 0
        };
      }

      var quantity = detail.quantity == 0 ? 1 : detail.quantity;

      return new OrderDetail
      {
        id = detail.id,
        order_id = detail.order_id,
        variant_id = detail.variant_id,
        quantity = quantity,
        variant = SanitizeVariant(detail.variant)
      };
    }

    private static Variant? SanitizeVariant(Variant? variant)
    {
      if (variant == null)
      {
        return null;
      }

      return new Variant
      {
        id = variant.id,
        product_id = variant.product_id,
        valuevariant = variant.valuevariant,
        stock = variant.stock,
        inputprice = variant.inputprice,
        price = variant.price,
        createdate = variant.createdate,
        updatedate = variant.updatedate,
        isdeleted = variant.isdeleted,
        product = SanitizeProduct(variant.product)
      };
    }

    private static Product? SanitizeProduct(Product? product)
    {
      if (product == null)
      {
        return null;
      }

      return new Product
      {
        id = product.id,
        nameproduct = product.nameproduct ?? string.Empty,
        brand_id = product.brand_id,
        description = product.description ?? string.Empty,
        categoryId = product.categoryId,
        imageurls = product.imageurls ?? Array.Empty<string>(),
        createdate = product.createdate,
        updatedate = product.updatedate,
        isdeleted = product.isdeleted
      };
    }

    private static Address SanitizeAddress(Address? address)
    {
      if (address == null)
      {
        return new Address
        {
          id = 0,
          accountid = 0,
          title = string.Empty,
          namerecipient = string.Empty,
          tel = string.Empty,
          codeward = 0,
          description = string.Empty,
          detail = string.Empty,
          createdate = null,
          updatedate = null
        };
      }

      return new Address
      {
        id = address.id,
        accountid = address.accountid,
        title = address.title ?? string.Empty,
        namerecipient = address.namerecipient ?? string.Empty,
        tel = address.tel ?? string.Empty,
        codeward = address.codeward,
        description = address.description ?? string.Empty,
        detail = address.detail ?? string.Empty,
        createdate = address.createdate,
        updatedate = address.updatedate
      };
    }

    private static Account SanitizeAccount(Account? account)
    {
      if (account == null)
      {
        return new Account
        {
          id = 0,
          email = string.Empty,
          lastname = string.Empty,
          firstname = string.Empty,
          bod = null,
          role = 0,
          avatarimg = string.Empty,
          createdate = null,
          updatedate = null,
          isdeleted = false
        };
      }

      return new Account
      {
        id = account.id,
        email = account.email ?? string.Empty,
        lastname = account.lastname ?? string.Empty,
        firstname = account.firstname ?? string.Empty,
        bod = account.bod,
        role = account.role,
        avatarimg = account.avatarimg ?? string.Empty,
        createdate = account.createdate,
        updatedate = account.updatedate,
        isdeleted = account.isdeleted
      };
    }

    private static OrderAdminDTO MapToDto(dotnet.Model.Order order, IReadOnlyList<dotnet.Model.OrderDetail> details)
    {
      var account = order.account;
      var address = order.address;

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
          Id = detail.id,
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

    private async Task<int> ResolveAddressAsync(int accountId, CreateOrderRequestDto request, CancellationToken cancellationToken)
    {
      if (request.AddressId.HasValue && request.AddressId.Value > 0)
      {
        _logger.LogInformation("Resolving address by ID: {AddressId} for account {AccountId}", request.AddressId.Value, accountId);
        var existingAddress = await _connect.address
          .FirstOrDefaultAsync(a => a.id == request.AddressId.Value && a.accountid == accountId, cancellationToken);

        if (existingAddress == null)
        {
          _logger.LogWarning("Address {AddressId} not found for account {AccountId}, will create new address", request.AddressId.Value, accountId);
          throw new KeyNotFoundException($"Address {request.AddressId.Value} not found for this account.");
        }

        _logger.LogInformation("Found existing address: {AddressId}", existingAddress.id);
        return existingAddress.id;
      }

      _logger.LogInformation("AddressId not provided, creating new address from CustomerInfo for account {AccountId}", accountId);
      var info = NormalizeCustomerInfo(request.CustomerInfo);
      var recipientName = $"{info.FirstName} {info.LastName}".Trim();
      if (string.IsNullOrWhiteSpace(recipientName))
      {
        recipientName = "Customer";
      }

      var address = new Address
      {
        accountid = accountId,
        title = string.IsNullOrWhiteSpace(info.Address) ? "Default" : "Checkout",
        namerecipient = recipientName,
        tel = info.Phone ?? string.Empty,
        codeward = 0,
        description = BuildAddressDescription(info),
        detail = info.Address,
        createdate = DateTime.UtcNow,
        updatedate = DateTime.UtcNow
      };

      _connect.address.Add(address);
      await _connect.SaveChangesAsync(cancellationToken);
      _logger.LogInformation("Created new address: {AddressId} for account {AccountId}", address.id, accountId);
      return address.id;
    }

    private static CustomerInfoDto NormalizeCustomerInfo(CustomerInfoDto? info)
    {
      if (info != null)
      {
        return info;
      }

      return new CustomerInfoDto
      {
        FirstName = "Customer",
        LastName = string.Empty,
        Phone = string.Empty,
        Country = "Vietnam",
        State = string.Empty,
        Address = string.Empty
      };
    }

    private static string BuildAddressDescription(CustomerInfoDto info)
    {
      var parts = new List<string>();

      if (!string.IsNullOrWhiteSpace(info.Address))
      {
        parts.Add(info.Address.Trim());
      }

      if (!string.IsNullOrWhiteSpace(info.State))
      {
        parts.Add(info.State.Trim());
      }

      if (!string.IsNullOrWhiteSpace(info.Country))
      {
        parts.Add(info.Country.Trim());
      }

      return string.Join(", ", parts);
    }
    // lấy ra trackOrder
    public async Task<ICollection<TrackOrderDTO>> getTrackOrder(int idAccount)
    {
      var sql = @$"
              SELECT
                  o.id            AS ""idOrder"",
                  o.account_id    AS ""idAccount"",
                  p.id            AS ""idProduct"",
                  o.statusorder   As ""status"",
                  p.nameproduct   AS ""nameProduct"",
                  p.description   AS ""description"",
                  p.imageurls     AS ""imgUrls"",
                  v.price         AS ""price"",
                  od.quantity     AS ""quantity"",
                  (od.quantity * v.price) AS ""subtotal""
              FROM orders o
              JOIN orderdetail od ON o.id = od.order_id
              JOIN variant v ON od.variant_id = v.id
              JOIN product p ON v.product_id = p.id
              WHERE o.account_id = {idAccount};";

      var rs = await _connect.trackOrderDTOs.FromSqlRaw(sql).ToListAsync();
      return rs;
    }

    public async Task<ICollection<TimeLineDTO>> getTimeLine(int idOrder, int idAccount)
    {
      var sql = @"
              SELECT
                  o.id            AS ""idOrder"",
                  o.orderdate     AS ""orderdate"",
                  o.receivedate   AS ""receivedate"",
                  o.statusorder   AS ""status"",
                  SUM(od.quantity) AS ""totalProduct""
              FROM orders o
              JOIN orderdetail od ON o.id = od.order_id
              WHERE o.account_id = {0}
              AND o.id = {1}
              GROUP BY o.id, o.orderdate, o.receivedate, o.statusorder;
              ";
      var rs = await _connect.timeLineDTOs.FromSqlRaw(sql, idAccount, idOrder).ToListAsync();
      return rs;
    }
  }
}
