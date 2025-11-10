using System.Collections.Generic;
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
      var sql = @"
            SELECT 
                a.id,
                a.firstname || ' ' || a.lastname AS name,
                a.email,
                a.role,
                COALESCE(a.avatarimg, '') AS avatarimg,
                COALESCE(
                    (SELECT tel FROM address ad WHERE ad.account_id = a.id LIMIT 1),
                    ''
                ) AS tel,
                COUNT(o.id) AS orders
            FROM account a
            LEFT JOIN orders o ON a.id = o.account_id
            GROUP BY a.id, a.firstname, a.lastname, a.email, a.role, a.avatarimg
            ORDER BY a.id;
        ";

      var list = _connect.Set<UserDTO>().FromSqlRaw(sql).AsNoTracking().ToList();
      return list;
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
