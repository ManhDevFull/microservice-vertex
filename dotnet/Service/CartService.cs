using dotnet.Dtos;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;

namespace dotnet.Service;

public class CartService : ICartService
{
    private readonly ICartRepository _repository;

    public CartService(ICartRepository repository)
    {
        _repository = repository;
    }

    public Task<CartResponseDto> GetCartAsync(int accountId)
    {
        return _repository.GetCartAsync(accountId);
    }

    public Task<CartResponseDto> AddOrIncrementAsync(int accountId, int variantId, int quantityDelta)
    {
        return _repository.AddOrIncrementAsync(accountId, variantId, quantityDelta);
    }

    public Task<CartResponseDto> UpdateQuantityAsync(int accountId, int cartId, int quantity)
    {
        return _repository.UpdateQuantityAsync(accountId, cartId, quantity);
    }

    public Task<CartResponseDto> RemoveAsync(int accountId, int cartId)
    {
        return _repository.RemoveAsync(accountId, cartId);
    }

    public Task<List<CartSuggestionDto>> GetSuggestionsAsync(int accountId, int offset, int limit, List<int> excludeProductIds)
    {
        return _repository.GetSuggestionsAsync(accountId, offset, limit, excludeProductIds);
    }
}

