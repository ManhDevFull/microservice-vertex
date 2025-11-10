using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Service.IService
{
    public interface IVariantService
    {
        public Task<List<VariantFilterDTO>> getValueVariant(int id);
        public Task<List<Variant>> GetVariantByFilter(FilterDTO dTO);
    }
}