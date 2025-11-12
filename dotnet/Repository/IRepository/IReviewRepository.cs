using System;
using System.Threading.Tasks;
using dotnet.Dtos.admin;

namespace dotnet.Repository.IRepository
{
  public interface IReviewRepository
  {
    Task<PagedResult<ReviewAdminDTO>> GetReviewsAsync(
        int page,
        int size,
        int? rating,
        bool? updated,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate);

    Task<ReviewAdminDTO?> GetReviewDetailAsync(int reviewId);
    Task<ReviewAdminSummaryDTO> GetSummaryAsync();
    Task<bool> UpdateReviewAsync(int reviewId, bool isUpdated);
  }
}
