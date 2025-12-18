using System;
using System.Threading.Tasks;
using dotnet.Dtos.admin;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;

namespace dotnet.Service
{
  public class ReviewService : IReviewService
  {
    private readonly IReviewRepository _repository;

    public ReviewService(IReviewRepository repository)
    {
      _repository = repository;
    }

    public Task<PagedResult<ReviewAdminDTO>> GetReviewsAsync(
        int page,
        int size,
        int? rating,
        bool? updated,
        string? keyword,
        DateTime? fromDate,
        DateTime? toDate)
    {
      return _repository.GetReviewsAsync(page, size, rating, updated, keyword, fromDate, toDate);
    }

    public Task<ReviewAdminDTO?> GetReviewDetailAsync(int reviewId)
    {
      return _repository.GetReviewDetailAsync(reviewId);
    }

    public Task<ReviewAdminSummaryDTO> GetSummaryAsync()
    {
      return _repository.GetSummaryAsync();
    }

    public Task<bool> UpdateReviewAsync(int reviewId, bool isUpdated)
    {
      return _repository.UpdateReviewAsync(reviewId, isUpdated);
    }
  }
}
