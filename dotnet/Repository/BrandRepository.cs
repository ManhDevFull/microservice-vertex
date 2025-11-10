using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Model;
using be_dotnet_ecommerce1.Repository.IRepository;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;

namespace be_dotnet_ecommerce1.Repository
{
    public class BrandRepository : IBrandRepository
    {
        private readonly ConnectData _connect;
        public BrandRepository(ConnectData connect){
            _connect = connect;
        }
        public async Task<List<Brand>> getAllBrand()
        {
            var rs = await _connect.brands.ToListAsync();
            return rs;
        }

        public async Task<List<Brand>> getBrandByProductIds(List<long> ids)
        {
            var rs = await _connect.brands.Where(b => ids.Contains(b.id)).ToListAsync();
            return rs;
        }



    }
}