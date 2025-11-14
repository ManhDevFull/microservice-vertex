using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Model;

namespace dotnet.Service.IService
{
  public interface IAddressService
  {
    public List<Address> getAddressByIdUser(int id);
  }
}