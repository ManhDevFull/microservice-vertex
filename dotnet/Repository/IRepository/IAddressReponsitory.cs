using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Model;

namespace dotnet.Repository.IRepository
{
  public interface IAddressReponsitory
  {
    public List<Address> getAddressByIdUser(int id);

    Task<IEnumerable<Address>> GetAddressesByUserIdAsync(int userId);

    Task<Address?> GetAddressByIdAsync(int id);

    Task<Address> CreateAddressAsync(Address address);

    Task<Address> UpdateAddressAsync(Address address);
    Task<bool> DeleteAddressAsync(int id);
  }
}