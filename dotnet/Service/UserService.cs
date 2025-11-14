using dotnet.Model;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace dotnet.Service
{
    public class UserService : IUserService
    {
        private readonly IUserReponsitory _repo;
        public UserService(IUserReponsitory repo)
        {
            _repo = repo;
        }

        public List<UserDTO> getUsers()
        {
            var list = _repo.getUserAdmin();
            return list;
        }

        public async Task<Account?> GetUserProfileByIdAsync(int userId)
        {
            return await _repo.GetByIdAsync(userId);
        }


        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _repo.GetByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(oldPassword, user.password); // Đã sửa thành user.password

            if (!isOldPasswordCorrect)
            {
                return false;
            }

            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.password = newPasswordHash; 

            user.updatedate = DateTime.UtcNow;

            if (user.createdate.HasValue && user.createdate.Value.Kind == DateTimeKind.Unspecified)
            {
                user.createdate = DateTime.SpecifyKind(user.createdate.Value, DateTimeKind.Utc);
            }
            if (user.bod.HasValue && user.bod.Value.Kind == DateTimeKind.Unspecified)
            {

                user.bod = DateTime.SpecifyKind(user.bod.Value, DateTimeKind.Utc);
            }
            if (user.refreshtokenexpires.HasValue && user.refreshtokenexpires.Value.Kind == DateTimeKind.Unspecified)
            {
                user.refreshtokenexpires = DateTime.SpecifyKind(user.refreshtokenexpires.Value, DateTimeKind.Utc);
            }
            _repo.Update(user);

            try 
            {
                await _repo.SaveChangesAsync(); 
                return true; 
            }
            catch (DbUpdateException ex)
            {

                Console.WriteLine($"DbUpdateException khi đổi mật khẩu: {ex.InnerException?.Message ?? ex.Message}");

                return false; 
            }
        }

        public async Task<Account?> UpdateProfileAsync(int userId, UserProfileUpdateDTO dto) // Đảm bảo UserProfileUpdateDTO đúng namespace
        {

            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.firstname = !string.IsNullOrEmpty(dto.FirstName) ? dto.FirstName : user.firstname;
            user.lastname = !string.IsNullOrEmpty(dto.LastName) ? dto.LastName : user.lastname;
            user.updatedate = DateTime.UtcNow;

            if (user.createdate.HasValue && user.createdate.Value.Kind == DateTimeKind.Unspecified)
            {
                user.createdate = DateTime.SpecifyKind(user.createdate.Value, DateTimeKind.Utc);
            }
            if (user.bod.HasValue && user.bod.Value.Kind == DateTimeKind.Unspecified)
            {
                user.bod = DateTime.SpecifyKind(user.bod.Value, DateTimeKind.Utc);
            }
            if (user.refreshtokenexpires.HasValue && user.refreshtokenexpires.Value.Kind == DateTimeKind.Unspecified)
            {
                user.refreshtokenexpires = DateTime.SpecifyKind(user.refreshtokenexpires.Value, DateTimeKind.Utc);
            }
            _repo.Update(user);

            try
            {
                await _repo.SaveChangesAsync();

                return user;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateAvatarUrlAsync(int userId, string avatarUrl)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.avatarimg = avatarUrl; 
            user.updatedate = DateTime.UtcNow; 

            if (user.createdate.HasValue && user.createdate.Value.Kind == DateTimeKind.Unspecified)
                user.createdate = DateTime.SpecifyKind(user.createdate.Value, DateTimeKind.Utc);
            if (user.bod.HasValue && user.bod.Value.Kind == DateTimeKind.Unspecified)
                user.bod = DateTime.SpecifyKind(user.bod.Value, DateTimeKind.Utc);
            if (user.refreshtokenexpires.HasValue && user.refreshtokenexpires.Value.Kind == DateTimeKind.Unspecified)
                user.refreshtokenexpires = DateTime.SpecifyKind(user.refreshtokenexpires.Value, DateTimeKind.Utc);

            _repo.Update(user); 
            await _repo.SaveChangesAsync();
            return true;
        }

    }
}
