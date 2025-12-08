using dotnet.Dtos;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class WishListController : ControllerBase
{
    private readonly IWishListService _service;
    private readonly ILogger<WishListController> _logger;

    public WishListController(IWishListService service, ILogger<WishListController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var accountId = ResolveAccountId();
        if (accountId == null) return Unauthorized();
        var data = await _service.GetAsync(accountId.Value);
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WishListAddDTO request)
    {
        var accountId = ResolveAccountId();
        if (accountId == null) return Unauthorized();
        var data = await _service.AddAsync(accountId.Value, request.productId);
        return Ok(data);
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var accountId = ResolveAccountId();
        if (accountId == null) return Unauthorized();
        var data = await _service.RemoveAsync(accountId.Value, productId);
        return Ok(data);
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
