using dotnet.Dtos;

namespace dotnet.Repository.IRepository;

public interface ICartRepository
{
    Task<CartResponseDto> GetCartAsync(int accountId);
    Task<CartResponseDto> AddOrIncrementAsync(int accountId, int variantId, int quantityDelta);
    Task<CartResponseDto> UpdateQuantityAsync(int accountId, int cartId, int quantity);
    Task<CartResponseDto> RemoveAsync(int accountId, int cartId);
    Task<List<CartSuggestionDto>> GetSuggestionsAsync(int accountId, int offset, int limit, List<int> excludeProductIds);
}

