using be_dotnet_ecommerce1.Data;
using dotnet.Model;
using dotnet.Repository.IRepository;

namespace dotnet.Repository
{
    public class AddressReponsitory : IAddressReponsitory
    {
    private readonly ConnectData _connect;
    public AddressReponsitory(ConnectData connect){
      _connect = connect;
    }
    public List<Address> getAddressByIdUser(int id){
      return _connect.address.Where(c => c.accountid == id).ToList();
    }
  }
}