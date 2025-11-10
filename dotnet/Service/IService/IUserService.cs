using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Model;

namespace dotnet.Service.IService
{
  public interface IUserService
  {
    List<UserDTO> getUsers();
    Task<Account?> GetUserProfileByIdAsync(int userId);
    Task<Account?> UpdateProfileAsync(int userId, UserProfileUpdateDTO dto);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<bool> UpdateAvatarUrlAsync(int userId, string avatarUrl);
  }
}
