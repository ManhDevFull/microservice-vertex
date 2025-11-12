using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using be_dotnet_ecommerce1.Data;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Model;
using dotnet.Repository.IRepository;

namespace dotnet.Repository
{
  public class UserReponsitory : IUserReponsitory
  {

    private readonly ConnectData _connect;
    public UserReponsitory(ConnectData connect)
    {
      _connect = connect;
    }
    public List<UserDTO> getUserAdmin()
    {
      var users = _connect.accounts
        .AsNoTracking()
        .Select(a => new UserDTO
        {
          id = a.id,
          name = ((a.firstname ?? string.Empty) + " " + (a.lastname ?? string.Empty)).Trim(),
          email = a.email ?? string.Empty,
          role = a.role,
          avatarImg = a.avatarimg,
          tel = a.addresses
            .OrderBy(ad => ad.id)
            .Select(ad => ad.tel)
            .FirstOrDefault() ?? string.Empty,
          orders = a.orders.Count()
        })
        .OrderBy(u => u.id)
        .ToList();

      return users;
    }

    public async Task<Account?> GetAccountByEmail(string email)
    {

      return await _connect.accounts.FirstOrDefaultAsync(u => u.email == email);
    }


    public async Task<Account?> GetByIdAsync(int userId)
    {
      return await _connect.accounts.FirstOrDefaultAsync(u => u.id == userId);
    }

    public async Task<bool> AddAccount(Account account)
    {

      _connect.accounts.Add(account);
      return await _connect.SaveChangesAsync() > 0;
    }
    public async Task<bool> UpdateAsync(Account account)
    {

      _connect.accounts.Update(account);
      return await _connect.SaveChangesAsync() > 0;
    }

    public void Update(Account user)
    {
      _connect.Update(user);
    }

    public async Task SaveChangesAsync()
    {
      await _connect.SaveChangesAsync();
    }
  }
}
