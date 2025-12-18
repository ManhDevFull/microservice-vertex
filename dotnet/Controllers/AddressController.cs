using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using dotnet.Dtos;
using dotnet.Service.IService;

namespace dotnet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize] // Yêu cầu đăng nhập
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;
        private readonly ILogger<AddressController> _logger;

        public AddressController(IAddressService addressService, ILogger<AddressController> logger)
        {
            _addressService = addressService;
            _logger = logger;
        }

        [HttpGet("my-addresses")]
        public async Task<IActionResult> GetMyAddresses()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            try
            {
                var addresses = await _addressService.GetAddressesByUserIdAsync(userId);
                return Ok(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách địa chỉ cho User ID {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] AddressCreateDTO dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newAddress = await _addressService.CreateAddressAsync(userId, dto);
                return Ok(newAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo địa chỉ cho User ID {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi server khi tạo địa chỉ." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressCreateDTO dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedAddress = await _addressService.UpdateAddressAsync(userId, id, dto);

                if (updatedAddress == null)
                {
                    return NotFound(new { message = "Không tìm thấy địa chỉ hoặc không có quyền." });
                }

                return Ok(updatedAddress);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật địa chỉ ID {AddressId}", id);
                return StatusCode(500, new { message = "Lỗi server khi cập nhật địa chỉ." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            try
            {
                var result = await _addressService.DeleteAddressAsync(userId, id);

                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy địa chỉ hoặc không có quyền." });
                }

                return Ok(new { message = "Đã ẩn địa chỉ thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa địa chỉ ID {AddressId}", id);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
