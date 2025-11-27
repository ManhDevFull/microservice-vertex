using be_dotnet_ecommerce1.Data;
using dotnet.Model;
using dotnet.Repository.IRepository;

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
      return await _context.address
          .Where(a => a.accountid == userId)
          .OrderByDescending(a => a.createdate) // Địa chỉ mới nhất lên đầu
          .ToListAsync();
    }

    public async Task<Address?> GetAddressByIdAsync(int id)
    {
      return await _context.address.FirstOrDefaultAsync(a => a.id == id);
    }

    public async Task<Address> CreateAddressAsync(Address address)
    {
      // Gán thời gian tạo
      address.createdate = DateTime.UtcNow;
      address.updatedate = DateTime.UtcNow;

      _context.address.Add(address);
      await _context.SaveChangesAsync();
      return address;
    }

    public async Task<Address> UpdateAddressAsync(Address address)
    {
      address.updatedate = DateTime.UtcNow;

      _context.address.Update(address);
      await _context.SaveChangesAsync();
      return address;
    }

    public async Task<bool> DeleteAddressAsync(int id)
    {
      var address = await _context.address.FirstOrDefaultAsync(a => a.id == id);
      if (address == null) return false;

      _context.address.Remove(address);
      await _context.SaveChangesAsync();
      return true;
    }
  }
}