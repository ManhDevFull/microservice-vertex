using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Data;
using dotnet.Dtos.admin;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace dotnet.Repository
{
  public class ReviewRepository : IReviewRepository
  {
    private readonly ConnectData _connect;

    public ReviewRepository(ConnectData connect)
    {
      _connect = connect;
    }

    public async Task<PagedResult<ReviewAdminDTO>> GetReviewsAsync(
        int page,
        int size,
        int? rating,
        bool? updated,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
      page = Math.Max(1, page);
      size = Math.Clamp(size, 1, 100);
      var offset = (page - 1) * size;

      var query = _connect.reviews
          .AsNoTracking()
          .Include(r => r.order)
            .ThenInclude(o => o.account)
          .Include(r => r.order)
            .ThenInclude(o => o.variant)
              .ThenInclude(v => v.product)
          .AsQueryable();

      if (rating.HasValue)
      {
        query = query.Where(r => r.rating == rating.Value);
      }

      if (updated.HasValue)
      {
        query = query.Where(r => r.isupdated == updated.Value);
      }

      if (fromDate.HasValue)
      {
        var from = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
        query = query.Where(r => r.createdate >= from);
      }

      if (toDate.HasValue)
      {
        var to = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
        query = query.Where(r => r.createdate < to);
      }

      if (!string.IsNullOrWhiteSpace(keyword))
      {
        var trimmed = keyword.Trim();
        var pattern = $"%{trimmed}%";
        query = query.Where(r =>
            EF.Functions.ILike(r.content ?? string.Empty, pattern) ||
            EF.Functions.ILike(r.order.account.firstname + " " + r.order.account.lastname, pattern) ||
            EF.Functions.ILike(r.order.account.email ?? string.Empty, pattern) ||
            EF.Functions.ILike(r.order.variant.product.nameproduct ?? string.Empty, pattern));
      }

      var total = await query.CountAsync();

      var reviews = await query
          .OrderByDescending(r => r.createdate)
          .Skip(offset)
          .Take(size)
          .ToListAsync();

      var items = reviews.Select(MapToDto).ToList();

      return new PagedResult<ReviewAdminDTO>
      {
        Items = items,
        Total = total,
        Page = page,
        Size = size
      };
    }

    public async Task<ReviewAdminDTO?> GetReviewDetailAsync(int reviewId)
    {
      var review = await _connect.reviews
          .AsNoTracking()
          .Include(r => r.order)
            .ThenInclude(o => o.account)
          .Include(r => r.order)
            .ThenInclude(o => o.variant)
              .ThenInclude(v => v.product)
          .FirstOrDefaultAsync(r => r.id == reviewId);

      return review == null ? null : MapToDto(review);
    }

    public async Task<ReviewAdminSummaryDTO> GetSummaryAsync()
    {
      var summary = new ReviewAdminSummaryDTO();

      summary.Total = await _connect.reviews.AsNoTracking().CountAsync();
      summary.Updated = await _connect.reviews.AsNoTracking().CountAsync(r => r.isupdated);

      summary.AverageRating = await _connect.reviews.AsNoTracking()
          .Select(r => (double?)r.rating)
          .AverageAsync() ?? 0;

      return summary;
    }

    public async Task<bool> UpdateReviewAsync(int reviewId, bool isUpdated)
    {
      var review = await _connect.reviews.FirstOrDefaultAsync(r => r.id == reviewId);
      if (review == null)
      {
        return false;
      }

      review.isupdated = isUpdated;
      review.updatedate = DateTime.UtcNow;

      await _connect.SaveChangesAsync();
      return true;
    }

    private static ReviewAdminDTO MapToDto(dotnet.Model.Review review)
    {
      var order = review.order;
      var account = order?.account;
      var variant = order?.variant;
      var product = variant?.product;

      return new ReviewAdminDTO
      {
        Id = review.id,
        OrderId = review.orderid,
        Rating = review.rating,
        Content = review.content ?? string.Empty,
        ImageUrls = review.imageurls?.ToList() ?? new List<string>(),
        CreateDate = review.createdate ?? DateTime.UtcNow,
        UpdateDate = review.updatedate ?? review.createdate,
        IsUpdated = review.isupdated,
        CustomerName = $"{(account?.firstname ?? string.Empty).Trim()} {(account?.lastname ?? string.Empty).Trim()}".Trim(),
        CustomerEmail = account?.email ?? string.Empty,
        ProductName = product?.nameproduct ?? string.Empty,
        ProductImage = product?.imageurls?.FirstOrDefault() ?? string.Empty,
        VariantAttributes = ExtractAttributes(variant?.valuevariant)
      };
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
          JsonValueKind.Number => property.Value.TryGetDecimal(out var decVal)
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
