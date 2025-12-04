using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Model;
<<<<<<< HEAD
using dotnet.Dtos;
=======

>>>>>>> user
namespace dotnet.Service.IService
{
  public interface IAddressService
  {
    public List<Address> getAddressByIdUser(int id);
<<<<<<< HEAD
    Task<IEnumerable<AddressDTO>> GetAddressesByUserIdAsync(int userId);

    Task<AddressDTO> CreateAddressAsync(int userId, AddressCreateDTO dto);
    Task<AddressDTO?> UpdateAddressAsync(int userId, int addressId, AddressCreateDTO dto);

    Task<bool> DeleteAddressAsync(int userId, int addressId);
=======
>>>>>>> user
  }
}