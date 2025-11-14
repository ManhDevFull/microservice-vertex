using Microsoft.AspNetCore.Authorization; // ✅ thêm để dùng [Authorize]
using Microsoft.AspNetCore.Mvc;
using be_dotnet_ecommerce1.Data;
using System.Security.Claims;
using dotnet.Service.IService;
using be_dotnet_ecommerce1.Dtos;
using Microsoft.Extensions.Logging;
using be.Service.IService;
namespace dotnet.Controllers
{
  [ApiController]
  [Route("[controller]")]
  [Authorize]
  public class UserController : ControllerBase
  {
    //  private readonly ConnectData _db;

    //   public UserController(ConnectData db)
    //   {
    //       _db = db;
    //   }

    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    private readonly IPhotoService _photoService;

    // ✅ Sửa Constructor để nhận IUserService
    public UserController(IUserService userService, ILogger<UserController> logger, IPhotoService photoService)
    {
      _userService = userService;
      _logger = logger;
      _photoService = photoService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult GetAll()
    {
      try
      {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return Ok(new
        {
          message = "User authenticated succes",
          userId,
          email,
          role
        });
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = ex });
      }
    }


    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
      var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
      {
        return Unauthorized("Không thể xác định người dùng từ token.");
      }
      var userAccount = await _userService.GetUserProfileByIdAsync(userId);

      if (userAccount == null)
      {
        return NotFound($"Không tìm thấy người dùng với ID: {userId}.");
      }

      var userProfileDto = new UserProfileDTO
      {
        Id = userAccount.id,
        Email = userAccount.email,
        FirstName = userAccount.firstname,
        LastName = userAccount.lastname,
        AvatarUrl = userAccount.avatarimg
      };

      return Ok(userProfileDto);
    }
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
    {
      var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (userIdString == null)
      {
        _logger.LogWarning("ChangePassword: Không tìm thấy NameIdentifier claim trong token."); // Ghi log cảnh báo
        return Unauthorized("Token không hợp lệ hoặc thiếu thông tin người dùng.");
      }
      // Nên dùng TryParse để an toàn hơn
      if (!int.TryParse(userIdString, out var userId))
      {
        _logger.LogWarning("ChangePassword: Không thể parse userId từ claim '{UserIdString}'.", userIdString);
        return Unauthorized("Thông tin người dùng trong token không hợp lệ.");
      }


      try
      {
        var result = await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);

        if (result)
        {
          _logger.LogInformation("User ID {UserId} đổi mật khẩu thành công.", userId); // Ghi log thành công
          return Ok(new { message = "Đổi mật khẩu thành công." });
        }
        else
        {
          _logger.LogWarning("User ID {UserId} đổi mật khẩu thất bại: Mật khẩu cũ không chính xác.", userId); // Ghi log thất bại
          return BadRequest(new { message = "Mật khẩu cũ không chính xác." });
        }
      }
      // Bắt lỗi cụ thể hơn nếu cần (ví dụ KeyNotFoundException từ Service)
      catch (KeyNotFoundException knfex)
      {
        _logger.LogWarning(knfex, "ChangePassword: Không tìm thấy user ID {UserId} khi đổi mật khẩu.", userId);
        return NotFound(new { message = "Không tìm thấy thông tin người dùng." }); // Trả về 404 Not Found
      }
      catch (Exception ex)
      {
        // Sử dụng _logger đã được inject
        _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho user ID {UserId}", userId);
        return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đổi mật khẩu." });
      }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateDTO dto)
    {
      var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
      {
        return Unauthorized("Không thể xác định người dùng từ token.");
      }

      try
      {
        var updatedUser = await _userService.UpdateProfileAsync(userId, dto);
        if (updatedUser == null)
        {
          _logger.LogWarning("UpdateProfile: Không tìm thấy user ID {UserId}.", userId);
          return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        // Map updatedUser sang DTO để trả về nếu cần, hoặc chỉ trả về Ok()
        _logger.LogInformation("Cập nhật profile thành công cho User ID {UserId}.", userId);
        return Ok(new { message = "Cập nhật profile thành công." });
      }
      catch (KeyNotFoundException)
      {
        return NotFound(new { message = "Không tìm thấy người dùng." });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Lỗi cập nhật profile cho user ID {UserId}", userId);
        return StatusCode(500, new { message = "Lỗi cập nhật profile." });
      }
    }

    [HttpPut("profile/avatar")]
    [HttpPost("avatar")]
    [Authorize] // Yêu cầu đăng nhập
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
      // Lấy userId từ token
      var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
      {
        return Unauthorized("Không thể xác định người dùng.");
      }

      // 1. Gửi file lên Cloudinary
      var uploadResult = await _photoService.AddPhotoAsync(file);

      // 2. Kiểm tra lỗi từ Cloudinary
      if (uploadResult.Error != null)
      {
        _logger.LogError("Lỗi upload Cloudinary: {Message}", uploadResult.Error.Message);
        return BadRequest(new { message = $"Lỗi upload ảnh: {uploadResult.Error.Message}" });
      }

      // 3. Lấy URL trả về
      string imageUrl = uploadResult.SecureUrl.ToString();

      // 4. Lấy PublicId (nếu bạn muốn lưu để sau này xóa)
      // string publicId = uploadResult.PublicId;

      try
      {
        // 5. Lưu URL vào database
        var success = await _userService.UpdateAvatarUrlAsync(userId, imageUrl);
        if (!success)
        {
          return NotFound(new { message = "Không tìm thấy người dùng để cập nhật ảnh." });
        }

        // 6. Trả về URL mới cho frontend
        return Ok(new
        {
          message = "Cập nhật avatar thành công.",
          avatarUrl = imageUrl
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Lỗi khi lưu URL avatar vào DB cho User ID {UserId}", userId);
        // (Tùy chọn) Nếu lưu DB lỗi, nên xóa ảnh vừa upload lên Cloudinary
        // await _photoService.DeletePhotoAsync(uploadResult.PublicId);
        return StatusCode(500, new { message = "Lỗi server khi lưu ảnh." });
      }
    }
  }
}
