using be_dotnet_ecommerce1.Model;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public interface IBrandRepository
    {
        public Task<List<Brand>> getBrandByProductIds(List<int> ids);
        public Task<List<Brand>> getAllBrand();
    }
}