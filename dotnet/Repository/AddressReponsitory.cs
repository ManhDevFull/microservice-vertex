using be_dotnet_ecommerce1.Data;
using dotnet.Model;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
namespace dotnet.Repository
{
  public class AddressReponsitory : IAddressReponsitory
  {
    private readonly ConnectData _connect;
    public AddressReponsitory(ConnectData connect)
    {
      _connect = connect;
    }
    public List<Address> getAddressByIdUser(int id)
    {
      return _connect.address.Where(c => c.accountid == id).ToList();
    }

    public async Task<IEnumerable<Address>> GetAddressesByUserIdAsync(int userId)
    {
      return await _connect.address
          .Where(a => a.accountid == userId)
          .OrderByDescending(a => a.createdate)
          .ToListAsync();
    }

    public async Task<Address?> GetAddressByIdAsync(int id)
    {
      return await _connect.address.FirstOrDefaultAsync(a => a.id == id);
    }

    public async Task<Address> CreateAddressAsync(Address address)
    {
      // Gán thời gian tạo
      address.createdate = DateTime.UtcNow;
      address.updatedate = DateTime.UtcNow;

      _connect.address.Add(address);
      await _connect.SaveChangesAsync();
      return address;
    }

    public async Task<Address> UpdateAddressAsync(Address address)
    {
      // Normalize timestamps to UTC to satisfy timestamptz
      address.createdate = DateTime.SpecifyKind(address.createdate ?? DateTime.UtcNow, DateTimeKind.Utc);
      address.updatedate = DateTime.UtcNow;

      _connect.address.Update(address);
      await _connect.SaveChangesAsync();
      return address;
    }

    public async Task<bool> DeleteAddressAsync(int id)
    {
      var address = await _connect.address.FirstOrDefaultAsync(a => a.id == id);
      if (address == null) return false;

      _connect.address.Remove(address);
      await _connect.SaveChangesAsync();
      return true;
    }
  }
}