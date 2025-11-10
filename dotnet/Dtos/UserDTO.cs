using Microsoft.EntityFrameworkCore;
namespace dotnet.Dtos
{
  [Keyless]
  public class UserDTO
  {
    public int id { get; set; } 
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int role { get; set; }
    public string? avatarImg { get; set; }
    public string? tel { get; set; }
    public int? orders { get; set; }
  }
}