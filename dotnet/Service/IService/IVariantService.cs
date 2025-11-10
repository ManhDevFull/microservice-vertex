using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Service.IService
{
    public interface IVariantService
    {
        public Task<List<VariantFilterDTO>> getValueVariant();
        public Task<List<V_VariantFilterDTO>> getAllVariant();
        public Task<List<VariantFilterDTO>> getValueVariantByNameCategory(string? name);
        public Task<List<Variant>> GetVariantByFilter(FilterDTO dTO);
    }
}