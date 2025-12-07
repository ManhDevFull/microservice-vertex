using dotnet.Dtos;

namespace dotnet.Repository.IRepository;

public interface IWishListRepository
{
    Task<List<WishListItemDTO>> GetAsync(int accountId);
    Task<List<WishListItemDTO>> AddAsync(int accountId, int productId);
    Task<List<WishListItemDTO>> RemoveAsync(int accountId, int productId);
}
