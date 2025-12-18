using dotnet.Dtos;

namespace dotnet.Service.IService;

public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(int accountId);
    Task<CartResponseDto> AddOrIncrementAsync(int accountId, int variantId, int quantityDelta);
    Task<CartResponseDto> UpdateQuantityAsync(int accountId, int cartId, int quantity);
    Task<CartResponseDto> RemoveAsync(int accountId, int cartId);
    Task<List<CartSuggestionDto>> GetSuggestionsAsync(int accountId, int offset, int limit, List<int> excludeProductIds);
}

