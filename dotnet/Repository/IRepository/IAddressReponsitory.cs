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
  }
}