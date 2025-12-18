using dotnet.Dtos;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;

namespace dotnet.Service;

public class WishListService : IWishListService
{
    private readonly IWishListRepository _repo;

    public WishListService(IWishListRepository repo)
    {
        _repo = repo;
    }

    public Task<List<WishListItemDTO>> GetAsync(int accountId) => _repo.GetAsync(accountId);
    public Task<List<WishListItemDTO>> AddAsync(int accountId, int productId) => _repo.AddAsync(accountId, productId);
    public Task<List<WishListItemDTO>> RemoveAsync(int accountId, int productId) => _repo.RemoveAsync(accountId, productId);
}
