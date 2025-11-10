using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public interface IVariantRepository
    {
        public Task<List<VariantFilterDTO>> GetValueVariant();
        public Task<List<VariantFilterDTO>> GetValueVariantByNameCategory(string? name);
        public Task<List<Variant>> GetVariantByFilter(FilterDTO dTO);
        public Task<Variant[]> GetVariantByIdProduct(int id);
        public Task<Variant[]> getVariantByIdProducts(List<int> ids);
        public Task<List<V_VariantFilterDTO>> getAllVariant();
    }
}