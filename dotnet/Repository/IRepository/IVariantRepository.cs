using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public interface IVariantRepository
    {
        public Task<List<VariantFilterDTO>> GetValueVariant(int id);
        public Task<List<Variant>> GetVariantByFilter(FilterDTO dTO);
    }
}