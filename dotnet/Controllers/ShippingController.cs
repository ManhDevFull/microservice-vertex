using System.Text.Json;
using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dotnet.Dtos.Cart;

namespace dotnet.Controllers;

[ApiController]
[Route("[controller]")]
public class ShippingController : ControllerBase
{
    private readonly Shipping.Grpc.PaymentRpc.PaymentRpcClient _paymentClient;
    private readonly Shipping.Grpc.ShippingRpc.ShippingRpcClient _shippingClient;
    private readonly Shipping.Grpc.CheckoutRpc.CheckoutRpcClient _checkoutClient;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(
        Shipping.Grpc.PaymentRpc.PaymentRpcClient paymentClient,
        Shipping.Grpc.ShippingRpc.ShippingRpcClient shippingClient,
        Shipping.Grpc.CheckoutRpc.CheckoutRpcClient checkoutClient,
        ILogger<ShippingController> logger)
    {
        _paymentClient = paymentClient;
        _shippingClient = shippingClient;
        _checkoutClient = checkoutClient;
        _logger = logger;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "ShippingController is working" });
    }

    [HttpGet("payment-providers")]
    public async Task<IActionResult> GetPaymentProviders()
    {
        try
        {
            var res = await _paymentClient.GetProvidersAsync(new Shipping.Grpc.Empty());

            var list = res.Items.Select(p => new
            {
                id = p.Id,
                code = p.Code,
                name = p.Name,
                description = p.Description,
                logoUrl = p.LogoUrl
            }).ToList();

            return Ok(list);
        }
        catch (RpcException rpcEx)
        {
            _logger.LogError(rpcEx, "gRPC error fetching payment providers");
            return StatusCode(500, new
            {
                error = "ShippingService connection failed",
                details = rpcEx.Status.Detail,
                statusCode = rpcEx.StatusCode.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment providers");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("carriers")]
    public async Task<IActionResult> GetCarriers()
    {
        try
        {
            var res = await _shippingClient.GetCarriersAsync(new Shipping.Grpc.Empty());
            var list = res.Items.Select(c => new
            {
                id = c.Code,
                name = c.Name,
                img = c.LogoUrl
            }).ToList();
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching carriers");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions([FromQuery] string carrierCode)
    {
        try
        {
            var res = await _shippingClient.GetOptionsAsync(new Shipping.Grpc.GetOptionsRequest { CarrierCode = carrierCode });
            var list = res.Items.Select(o => new
            {
                id = o.Id.ToString(),
                carrierId = o.CarrierId,
                code = o.Code,
                name = o.Name,
                deliveryTime = $"{o.DeliveryMinDays}-{o.DeliveryMaxDays} days",
                shippingCost = o.ShippingCost == 0 ? "Free" : $"₹{o.ShippingCost}",
                insurance = o.InsuranceAvailable ? "Available" : "Unavailable"
            }).ToList();
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shipping options");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("carriers-with-options")]
    public async Task<IActionResult> GetCarriersWithOptions()
    {
        try
        {
            var carriersRes = await _shippingClient.GetCarriersAsync(new Shipping.Grpc.Empty());

            var result = new List<object>();

            foreach (var carrier in carriersRes.Items)
            {
                var optsRes = await _shippingClient.GetOptionsAsync(
                    new Shipping.Grpc.GetOptionsRequest { CarrierCode = carrier.Code });

                var options = optsRes.Items
                    .Select(o => new
                    {
                        id = o.Id,
                        code = o.Code,
                        name = o.Name,
                        deliveryMinDays = o.DeliveryMinDays,
                        deliveryMaxDays = o.DeliveryMaxDays,
                        shippingCost = o.ShippingCost,
                        insuranceAvailable = o.InsuranceAvailable
                    })
                    .ToList();

                if (options.Count == 0)
                {
                    continue;
                }

                result.Add(new
                {
                    id = carrier.Code,
                    name = carrier.Name,
                    logoUrl = carrier.LogoUrl,
                    options
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching carriers with options");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("checkout-selection")]
    public async Task<IActionResult> SaveSelection([FromBody] SaveSelectionRequest req)
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var accountId))
            {
                return Unauthorized();
            }

            var dto = new Shipping.Grpc.CheckoutSelectionDto
            {
                AccountId = accountId,
                PaymentProviderId = req.PaymentProviderId,
                ShippingOptionId = req.ShippingOptionId,
                AddressSnapshot = JsonSerializer.Serialize(req.AddressSnapshot)
            };

            var res = await _checkoutClient.SaveSelectionAsync(dto);
            return Ok(new { id = res.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving checkout selection");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("checkout-selection")]
    public async Task<IActionResult> GetSelection()
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var accountId))
            {
                return Unauthorized();
            }

            var res = await _checkoutClient.GetSelectionAsync(new Shipping.Grpc.GetSelectionRequest
            {
                AccountId = accountId
            });

            var snapshot = string.IsNullOrWhiteSpace(res.AddressSnapshot)
                ? null
                : JsonSerializer.Deserialize<object>(res.AddressSnapshot);

            return Ok(new
            {
                id = res.Id,
                paymentProviderId = res.PaymentProviderId,
                shippingOptionId = res.ShippingOptionId,
                addressSnapshot = snapshot
            });
        }
        catch (RpcException rpcEx) when (rpcEx.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checkout selection");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

