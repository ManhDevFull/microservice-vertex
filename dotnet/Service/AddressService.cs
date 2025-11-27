using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Model;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;
using dotnet.Dtos;
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
      var list = _repo.getAddressByIdUser(id);
      return list;
    }

    // --- TRIỂN KHAI CÁC HÀM MỚI ---

    public async Task<IEnumerable<AddressDTO>> GetAddressesByUserIdAsync(int userId)
    {
      var addresses = await _repo.GetAddressesByUserIdAsync(userId);

      // Map từ Model sang DTO
      return addresses.Select(a => new AddressDTO
      {
        Id = a.id,
        Title = a.title,
        NameRecipient = a.namerecipient,
        Tel = a.tel,
        CodeWard = a.codeward,
        Detail = a.detail,
        Description = a.description
        // Frontend sẽ tự dùng CodeWard để hiển thị địa chỉ đầy đủ
      });
    }

    public async Task<AddressDTO> CreateAddressAsync(int userId, AddressCreateDTO dto)
    {
      // Map từ CreateDTO sang Model
      var address = new Address
      {
        accountid = userId,
        title = dto.Title,
        namerecipient = dto.NameRecipient,
        tel = dto.Tel,
        codeward = dto.CodeWard,
        detail = dto.Detail,
        description = dto.Description
      };

      var createdAddress = await _repo.CreateAddressAsync(address);

      // Trả về DTO
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
      // 1. Kiểm tra địa chỉ có tồn tại và thuộc về user này không
      var existingAddress = await _repo.GetAddressByIdAsync(addressId);
      if (existingAddress == null || existingAddress.accountid != userId)
      {
        return null; // Không tìm thấy hoặc không có quyền
      }

      // 2. Cập nhật thông tin
      existingAddress.title = dto.Title;
      existingAddress.namerecipient = dto.NameRecipient;
      existingAddress.tel = dto.Tel;
      existingAddress.codeward = dto.CodeWard;
      existingAddress.detail = dto.Detail;
      existingAddress.description = dto.Description;

      // 3. Lưu xuống DB
      var updatedAddress = await _repo.UpdateAddressAsync(existingAddress);

      // 4. Trả về DTO
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
      // Kiểm tra quyền sở hữu trước khi xóa
      var existingAddress = await _repo.GetAddressByIdAsync(addressId);
      if (existingAddress == null || existingAddress.accountid != userId)
      {
        return false;
      }
      return await _repo.DeleteAddressAsync(addressId);
    }
  }
}