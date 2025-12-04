using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using dotnet.Dtos;
using dotnet.Service.IService;

namespace dotnet.Controllers
{
    [ApiController]
    [Route("[controller]")] // API sẽ là: http://localhost:5200/Address
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

        // 1. Lấy danh sách địa chỉ của tôi
        // GET: /Address/my-addresses
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

        // 2. Thêm địa chỉ mới
        // POST: /Address
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

        // 3. Cập nhật địa chỉ
        // PUT: /Address/{id}
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
                    return NotFound(new { message = "Không tìm thấy địa chỉ hoặc bạn không có quyền sửa." });
                }

                return Ok(updatedAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật địa chỉ ID {AddressId}", id);
                return StatusCode(501, new { message = "Lỗi server khi cập nhật địa chỉ." });
            }
        }

        // 4. Xóa địa chỉ
        // DELETE: /Address/{id}
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
                    return NotFound(new { message = "Không tìm thấy địa chỉ hoặc bạn không có quyền xóa." });
                }

                return Ok(new { message = "Xóa địa chỉ thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa địa chỉ ID {AddressId}", id);
                return StatusCode(500, new { message = "Lỗi server khi xóa địa chỉ." });
            }
        }
    }
}