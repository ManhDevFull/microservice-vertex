using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Model;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;

namespace dotnet.Service
{
  public class AddressService : IAddressService
  {
    private readonly IAddressReponsitory _repo;

    public AddressService(IAddressReponsitory repo)
    {
      _repo = repo;
    }

    public List<Address> getAddressByIdUser(int id)
    {
      return _repo.getAddressByIdUser(id);
    }

    public async Task<IEnumerable<AddressDTO>> GetAddressesByUserIdAsync(int userId)
    {
      var addresses = await _repo.GetAddressesByUserIdAsync(userId);

      return addresses.Select(a => new AddressDTO
      {
        Id = a.id,
        Title = a.title,
        NameRecipient = a.namerecipient,
        Tel = a.tel,
        CodeWard = a.codeward,
        Detail = a.detail,
        Description = a.description
      });
    }

    public async Task<AddressDTO> CreateAddressAsync(int userId, AddressCreateDTO dto)
    {
      var address = new Address
      {
        accountid = userId,
        title = dto.Title,
        namerecipient = dto.NameRecipient,
        tel = dto.Tel,
        codeward = dto.CodeWard,
        detail = dto.Detail,
        description = dto.Description,
        isdeleted = false
      };

      var createdAddress = await _repo.CreateAddressAsync(address);

      return new AddressDTO
      {
        Id = createdAddress.id,
        Title = createdAddress.title,
        NameRecipient = createdAddress.namerecipient,
        Tel = createdAddress.tel,
        CodeWard = createdAddress.codeward,
        Detail = createdAddress.detail,
        Description = createdAddress.description
      };
    }

    public async Task<AddressDTO?> UpdateAddressAsync(int userId, int addressId, AddressCreateDTO dto)
    {
      var existingAddress = await _repo.GetAddressWithOrdersAsync(addressId);
      if (existingAddress == null || existingAddress.accountid != userId || existingAddress.isdeleted == true)
      {
        return null;
      }

      if (existingAddress.orders != null && existingAddress.orders.Any())
      {
        throw new InvalidOperationException("Địa chỉ đã được sử dụng cho đơn hàng, không thể chỉnh sửa.");
      }

      existingAddress.title = dto.Title;
      existingAddress.namerecipient = dto.NameRecipient;
      existingAddress.tel = dto.Tel;
      existingAddress.codeward = dto.CodeWard;
      existingAddress.detail = dto.Detail;
      existingAddress.description = dto.Description;
      existingAddress.updatedate = DateTime.UtcNow;

      var updatedAddress = await _repo.UpdateAddressAsync(existingAddress);

      return new AddressDTO
      {
        Id = updatedAddress.id,
        Title = updatedAddress.title,
        NameRecipient = updatedAddress.namerecipient,
        Tel = updatedAddress.tel,
        CodeWard = updatedAddress.codeward,
        Detail = updatedAddress.detail,
        Description = updatedAddress.description
      };
    }

    public async Task<bool> DeleteAddressAsync(int userId, int addressId)
    {
      var existingAddress = await _repo.GetAddressByIdAsync(addressId);
      if (existingAddress == null || existingAddress.accountid != userId)
      {
        return false;
      }

      if (existingAddress.isdeleted == true)
      {
        return true;
      }

      return await _repo.DeleteAddressAsync(addressId);
    }
  }
}
