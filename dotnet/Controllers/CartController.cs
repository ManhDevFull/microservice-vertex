using dotnet.Dtos;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _service;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService service, ILogger<CartController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var accountId = ResolveAccountId();
        if (accountId == null)
        {
            return Unauthorized();
        }

        var data = await _service.GetCartAsync(accountId.Value);
        return Ok(data);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] CartAddDto request)
    {
        var accountId = ResolveAccountId();
        if (accountId == null)
        {
            return Unauthorized();
        }

        var data = await _service.AddOrIncrementAsync(accountId.Value, request.variantId, request.quantity);
        return Ok(data);
    }

    [HttpPatch("quantity")]
    public async Task<IActionResult> UpdateQuantity([FromBody] CartUpdateQtyDto request)
    {
        var accountId = ResolveAccountId();
        if (accountId == null)
        {
            return Unauthorized();
        }

        var data = await _service.UpdateQuantityAsync(accountId.Value, request.cartId, request.quantity);
        return Ok(data);
    }

    [HttpDelete("{cartId}")]
    public async Task<IActionResult> Remove(int cartId)
    {
        var accountId = ResolveAccountId();
        if (accountId == null)
        {
            return Unauthorized();
        }

        var data = await _service.RemoveAsync(accountId.Value, cartId);
        return Ok(data);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int offset = 0, [FromQuery] int limit = 12, [FromQuery] string? exclude = null)
    {
        var accountId = ResolveAccountId();
        if (accountId == null)
        {
            return Unauthorized();
        }

        var excludeIds = new List<int>();
        if (!string.IsNullOrWhiteSpace(exclude))
        {
            excludeIds = exclude.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(token => int.TryParse(token, out var id) ? id : (int?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
        }

        var suggestions = await _service.GetSuggestionsAsync(accountId.Value, offset, limit, excludeIds);
        return Ok(suggestions);
    }

    private int? ResolveAccountId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var accountId))
        {
            _logger.LogWarning("Unable to resolve account id from token");
            return null;
        }

        return accountId;
    }
}

