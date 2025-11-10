using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Model;
using dotnet.Repository.IRepository;
using dotnet.Service.IService;

namespace dotnet.Service
{
    public class AddressService : IAddressService
    {
    private readonly IAddressReponsitory _repo;
    public AddressService(IAddressReponsitory repo){
      _repo = repo;
    }
    public List<Address> getAddressByIdUser(int id){
      var list = _repo.getAddressByIdUser(id);
      return list;
    }
  }
}