using dotnet.Dtos;

namespace dotnet.Service.IService;

public interface IWishListService
{
    Task<List<WishListItemDTO>> GetAsync(int accountId);
    Task<List<WishListItemDTO>> AddAsync(int accountId, int productId);
    Task<List<WishListItemDTO>> RemoveAsync(int accountId, int productId);
}
