using Microsoft.AspNetCore.Mvc;
using be_dotnet_ecommerce1.Data;
using dotnet.Model;
using be_dotnet_ecommerce1.Model;
using dotnet.Dtos;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Net.Http.Json; // NEW CODE: use HttpClient extensions for JSON payloads
using System.Text.Json; // NEW CODE: parse Verify service responses

namespace dotnet.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class AuthController : ControllerBase
  {
    // OLD: private readonly NpgsqlDataSource _dataSource;
    private readonly ConnectData _db; // NEW
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly ILogger<AuthController> _logger;

    // OLD:
    // public AuthController(NpgsqlDataSource dataSource, IConfiguration config, IHttpClientFactory httpClientFactory)
    // {
    //     _dataSource = dataSource;
    //     _config = config;
    //     _http = httpClientFactory.CreateClient();
    // }

    // NEW:
    public AuthController(
      ConnectData db,
      IConfiguration config,
      IHttpClientFactory httpClientFactory,
      ILogger<AuthController> logger)
    {
      _db = db;
      _config = config;
      _http = httpClientFactory.CreateClient();
      _logger = logger;
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
      try
      {
        var user = await _db.accounts.FirstOrDefaultAsync(u => u.email == dto.Email);
        if (user == null)
          return Unauthorized(new { message = "Email not found" });

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.password))
          return Unauthorized(new { message = "Invalid password" });

        var accessToken = GenerateJwtToken(user.id.ToString(), dto.Email ?? string.Empty, user.role.ToString());
        var refreshToken = GenerateRefreshToken();

        user.refreshtoken = refreshToken;
        user.refreshtokenexpires = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();


        var accessCookieOptions = new CookieOptions
        {
          HttpOnly = true,
          Secure = false,
          SameSite = SameSiteMode.Lax,
          Expires = DateTimeOffset.UtcNow.AddMinutes(15),
          Path = "/"
        };
        Response.Cookies.Append("accessToken", accessToken, accessCookieOptions);

        // cookie for refresh token (long lived)
        var refreshCookieOptions = new CookieOptions
        {
          HttpOnly = true,
          Secure = false,
          SameSite = SameSiteMode.Lax,
          Expires = DateTimeOffset.UtcNow.AddDays(7),
          Path = "/"
        };
        Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);

        return Ok(new
        {
          status = 200,
          data = new
          {
            accessToken,
            user = new { id = user.id, name = $"{user.firstname} {user.lastname}", email = dto.Email, avatarUrl = user.avatarimg, rule = user.role }
          }
        });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
      try
      {
        var cookieRt = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(cookieRt))
        {
          var user = await _db.accounts.FirstOrDefaultAsync(u => u.refreshtoken == cookieRt);
          if (user != null)
          {
            user.refreshtoken = null;
            user.refreshtokenexpires = null;

            _db.Entry(user).Property(u => u.refreshtoken).IsModified = true;
            _db.Entry(user).Property(u => u.refreshtokenexpires).IsModified = true;

            var result = await _db.SaveChangesAsync();
            Console.WriteLine($"Rows affected: {result}");

          }
        }

        Response.Cookies.Delete("refreshToken");
        Response.Cookies.Delete("accessToken");

        return Ok(new { status = 200, message = "Logged out successfully" });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    [HttpPost("social-auth")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeFirebaseToken([FromBody] TokenRequest dto)
    {
      if (string.IsNullOrEmpty(dto.IdToken))
        return BadRequest(new { message = "Missing Firebase IdToken" });
      try
      {
        var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(dto.IdToken);

        var uid = decoded.Uid;
        var email = decoded.Claims.ContainsKey("email") ? decoded.Claims["email"]?.ToString() : null;
        var name = decoded.Claims.ContainsKey("name") ? decoded.Claims["name"]?.ToString() : null;
        var avatarUrl = decoded.Claims.ContainsKey("picture") ? decoded.Claims["picture"]?.ToString() : null;

        var user = await _db.accounts.FirstOrDefaultAsync(u => u.email == email);
        if (user == null)
        {
          var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
          var fn = parts.Length > 0 ? parts[0] : "";
          var ln = parts.Length > 1 ? parts[^1] : "";

          user = new Account
          {
            email = email ?? $"{uid}@firebase.com",
            firstname = fn,
            lastname = ln,
            avatarimg = avatarUrl,
            createdate = DateTime.UtcNow,// moi hoi chat phan nay loi gi ben sql datetime utc
            role = 3
          };
          _db.Add(user);
          await _db.SaveChangesAsync();
        }

        var accessToken = GenerateJwtToken(user.id.ToString(), user.email ?? string.Empty, user.role.ToString());
        var refreshToken = GenerateRefreshToken();

        user.refreshtoken = refreshToken;
        user.refreshtokenexpires = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        var cookieOptions = new CookieOptions
        {
          HttpOnly = true,
          Secure = false,
          SameSite = SameSiteMode.Strict,
          Expires = DateTimeOffset.UtcNow.AddDays(7),
          Path = "/"
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        var displayName = string.Join(" ", new[] { user.firstname, user.lastname }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(displayName))
          displayName = name ?? string.Empty;

        var displayAvatar = string.IsNullOrWhiteSpace(user.avatarimg) ? avatarUrl : user.avatarimg;

        return Ok(new
        {
          status = 200,
          data = new { accessToken, user = new { id = user.id, name = displayName, avatarUrl = displayAvatar, email, rule = user.role } }
        });
      }
      catch (Exception ex)
      {
        return Unauthorized(new { message = "Invalid Firebase IdToken", detail = ex.Message });
      }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest dto)
    {
      try
      {
        var providedRt = dto?.RefreshToken;
        var cookieRt = Request.Cookies["refreshToken"];
        var refreshTokenToCheck = !string.IsNullOrEmpty(providedRt) ? providedRt : cookieRt;

        if (string.IsNullOrEmpty(refreshTokenToCheck))
          return Unauthorized(new { message = "No refresh token provided" });

        // OLD: Npgsql
        // await using var conn = await _dataSource.OpenConnectionAsync();
        // await using var cmd = new NpgsqlCommand("SELECT _id, email, rule, refresh_token_expires FROM account WHERE refresh_token = @rt", conn);
        // cmd.Parameters.AddWithValue("rt", refreshTokenToCheck);

        // NEW: EF Core
        var user = await _db.accounts.FirstOrDefaultAsync(u => u.refreshtoken == refreshTokenToCheck);
        if (user == null || user.refreshtokenexpires < DateTime.UtcNow)
          return Unauthorized(new { message = "Invalid or expired refresh token" });

        var newRefreshToken = GenerateRefreshToken();
        user.refreshtoken = newRefreshToken;
        user.refreshtokenexpires = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        var newAccessToken = GenerateJwtToken(user.id.ToString(), user.email ?? string.Empty, user.role.ToString());

        var cookieOptions = new CookieOptions
        {
          HttpOnly = true,
          Secure = true,
          SameSite = SameSiteMode.Strict,
          Expires = DateTimeOffset.UtcNow.AddDays(7),
          Path = "/"
        };
        Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

        return Ok(new { status = 200, data = new { accessToken = newAccessToken } });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    // NEW CODE: proxy Verify service to request an OTP for user registration
    [HttpPost("register/send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendRegistrationOtp([FromBody] RegisterRequest dto)
    {
      var cancellationToken = HttpContext.RequestAborted;
      if (dto == null)
        return BadRequest(new { message = "Request body is required." });

      var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
      var password = dto.Password ?? string.Empty;
      var fullName = (dto.FullName ?? string.Empty).Trim();

      if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
        return BadRequest(new { message = "Email, password and full name are required." });

      if (!IsValidEmail(email))
        return BadRequest(new { message = "Invalid email format." });

      var alreadyExists = await _db.accounts.AnyAsync(a => a.email == email, cancellationToken);
      if (alreadyExists)
        return Conflict(new { message = "Email already registered." });

      var baseUrl = GetVerifyServiceBaseUrl();
      if (string.IsNullOrEmpty(baseUrl))
      {
        _logger.LogError("Verify service base URL is not configured.");
        return StatusCode(500, new { message = "OTP service is not configured." });
      }
_logger.LogInformation("Verify baseUrl = `{BaseUrl}`", baseUrl);

      try
      {
        var response = await _http.PostAsJsonAsync($"{baseUrl}/otp/send", new { email }, cancellationToken);
        var rawBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
          var errorMessage = ExtractProblemDetail(rawBody) ?? "Failed to send OTP.";
          _logger.LogWarning("Verify service send OTP failure ({Status}): {Detail}", response.StatusCode, errorMessage);
          return StatusCode((int)response.StatusCode, new { message = errorMessage });
        }

        VerifyServiceSendResponse? payload = null;
        try
        {
          payload = JsonSerializer.Deserialize<VerifyServiceSendResponse>(rawBody, VerifyJsonOptions);
        }
        catch (JsonException jsonEx)
        {
          _logger.LogWarning(jsonEx, "Failed to parse Verify service send response for {Email}.", email);
        }

        return Ok(new
        {
          status = 200,
          message = "Verification code sent successfully.",
          data = new
          {
            email,
            expiresAt = payload?.ExpiresAt
          }
        });
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        _logger.LogError(ex, "Unexpected error while sending OTP for {Email}", email);
        return StatusCode(500, new
        {
          message = "Unexpected error while sending OTP.",
          detail = ex.Message
        });
      }
    }

    // NEW CODE: verify OTP with Verify service and create the account
    [HttpPost("register/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest dto)
    {
      var cancellationToken = HttpContext.RequestAborted;
      if (dto == null)
        return BadRequest(new { message = "Request body is required." });

      var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
      var password = dto.Password ?? string.Empty;
      var fullName = (dto.FullName ?? string.Empty).Trim();
      var code = (dto.Code ?? string.Empty).Trim();

      if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(code))
        return BadRequest(new { message = "Email, password, full name and OTP code are required." });

      if (!IsValidEmail(email))
        return BadRequest(new { message = "Invalid email format." });

      if (code.Length != 6 || !code.All(char.IsDigit))
        return BadRequest(new { message = "OTP code must be a 6-digit number." });

      var existingAccount = await _db.accounts.FirstOrDefaultAsync(a => a.email == email, cancellationToken);
      if (existingAccount != null)
        return Conflict(new { message = "Email already registered." });

      var baseUrl = GetVerifyServiceBaseUrl();
      if (string.IsNullOrEmpty(baseUrl))
      {
        _logger.LogError("Verify service base URL is not configured.");
        return StatusCode(500, new { message = "OTP service is not configured." });
      }

      try
      {
        var verifyResponse = await _http.PostAsJsonAsync($"{baseUrl}/otp/verify", new { Email = email, Code = code }, cancellationToken);
        var rawBody = await verifyResponse.Content.ReadAsStringAsync();

        VerifyServiceVerifyResponse? payload = null;
        try
        {
          payload = JsonSerializer.Deserialize<VerifyServiceVerifyResponse>(rawBody, VerifyJsonOptions);
        }
        catch (JsonException jsonEx)
        {
          _logger.LogWarning(jsonEx, "Failed to parse Verify service verify response for {Email}.", email);
        }

        if (!verifyResponse.IsSuccessStatusCode || payload?.Verified != true)
        {
          var errorMessage = payload?.Error ?? ExtractProblemDetail(rawBody) ?? "OTP verification failed.";
          _logger.LogWarning("OTP verification failed for {Email}: {Message}", email, errorMessage);
          return StatusCode((int)verifyResponse.StatusCode, new { message = errorMessage, verified = payload?.Verified ?? false });
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var (firstName, lastName) = SplitFullName(fullName);
        var utcNow = DateTime.UtcNow;

        var account = new Account
        {
          email = email,
          password = hashedPassword,
          firstname = firstName,
          lastname = lastName,
          createdate = utcNow,
          updatedate = utcNow,
          role = 3
        };

        _db.accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
          status = 200,
          message = "Registration completed successfully.",
          data = new { email }
        });
      }
      catch (DbUpdateException dbEx)
      {
        _logger.LogError(dbEx, "Database error while creating account for {Email}", email);
        return StatusCode(500, new { message = "Failed to create account. Please try again." });
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        _logger.LogError(ex, "Unexpected error while verifying OTP for {Email}", email);
        return StatusCode(500, new
        {
          message = "Unexpected error while verifying OTP.",
          detail = ex.Message
        });
      }
    }

    // NEW CODE: value objects + helpers for Verify service integration
    private static readonly JsonSerializerOptions VerifyJsonOptions = new(JsonSerializerDefaults.Web)
    {
      PropertyNameCaseInsensitive = true
    };

    private sealed record VerifyServiceSendResponse(string Email, DateTimeOffset ExpiresAt);

    private sealed record VerifyServiceVerifyResponse(string Email, bool Verified, string? Error);

    private string? GetVerifyServiceBaseUrl()
    {
      var url = _config["Services:Verify:BaseUrl"];
      if (string.IsNullOrWhiteSpace(url))
        return "https://verifyemail-cl42.onrender.com";

      var trimmed = url.TrimEnd('/');
      if (trimmed.Contains("localhost", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("5001", StringComparison.Ordinal))
        return "https://verifyemail-cl42.onrender.com";

      return trimmed;
    }

    private static string? ExtractProblemDetail(string? rawBody)
    {
      if (string.IsNullOrWhiteSpace(rawBody))
        return null;

      try
      {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
          return detail.GetString();
        if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
          return message.GetString();
        if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
          return error.GetString();
        if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
          return title.GetString();
      }
      catch (JsonException)
      {
        // ignore parse errors and return raw body instead
      }

      return rawBody;
    }

    // ===== Helper functions =====
    private string GenerateRefreshToken()
    {
      return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }

    private string GenerateJwtToken(string userId, string email, string rule)
    {
      var jwtKey = _config["Jwt:Key"];
      var jwtIssuer = _config["Jwt:Issuer"];
      var jwtAudience = _config["Jwt:Audience"];

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

      var claims = new[]
      {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rule),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

      var token = new JwtSecurityToken(
          issuer: jwtIssuer,
          audience: jwtAudience,
          claims: claims,
          expires: DateTime.UtcNow.AddMinutes(60),
          signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateVerificationCode()
    {
      Span<byte> buffer = stackalloc byte[4];
      RandomNumberGenerator.Fill(buffer);
      var value = BitConverter.ToUInt32(buffer) % 1000000;
      return value.ToString("D6");
    }

    private static (string firstName, string lastName) SplitFullName(string fullName)
    {
      var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
        return (string.Empty, string.Empty);
      if (parts.Length == 1)
        return (parts[0], string.Empty);
      var firstName = parts[0];
      var lastName = string.Join(' ', parts.Skip(1));
      return (firstName, lastName);
    }

    private bool IsValidEmail(string email)
    {
      try
      {
        var address = new MailAddress(email);
        return address.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
      }
      catch
      {
        return false;
      }
    }

    private string BuildVerificationEmailBody(string fullName, string code, int expiryMinutes)
    {
      var safeName = string.IsNullOrWhiteSpace(fullName) ? "there" : WebUtility.HtmlEncode(fullName);
      var safeCode = WebUtility.HtmlEncode(code);
      var expiryText = expiryMinutes <= 1 ? "1 minute" : $"{expiryMinutes} minutes";

      return $@"
<p>Hi {safeName},</p>
<p>Your verification code is <strong style=""font-size:20px;"">{safeCode}</strong>.</p>
<p>This code will expire in {expiryText}. If you did not request this, you can safely ignore this email.</p>
<p>Thanks,<br/>Vertex E-commerce Team</p>";
    }
  }
}
