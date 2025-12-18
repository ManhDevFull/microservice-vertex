using Microsoft.EntityFrameworkCore;
namespace dotnet.Dtos.admin
{
  [Keyless]
  public class UserAdminDTO
  {
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public int role { get; set; }
    public string? avatarImg { get; set; }
    public string? tel { get; set; }
    public long? orders { get; set; }
  }
}
