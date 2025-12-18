using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Model;

namespace dotnet.Repository.IRepository
{
  public interface IUserReponsitory
  {
    List<UserDTO> getUserAdmin();
    Task<Account?> GetByIdAsync(int userId);
    Task<Account?> GetAccountByEmail(string email);
    Task<bool> AddAccount(Account account);
    Task<bool> UpdateAsync(Account account);
    void Update(Account user);
    Task SaveChangesAsync();
  }
}
