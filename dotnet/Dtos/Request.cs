namespace dotnet.Dtos
{
  public class LoginRequest
  {
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
  }

  public class TokenRequest
  {
    public string IdToken { get; set; } = null!;
  }

  public class RefreshTokenRequest
  {
    public string RefreshToken { get; set; } = "";
  }

  public class RegisterRequest
  {
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
  }

  public class VerifyEmailRequest
  {
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
  }

  // NEW CODE: request body for completing sign-up after OTP verification
  public class CompleteRegistrationRequest
  {
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Code { get; set; } = "";
  }
}
