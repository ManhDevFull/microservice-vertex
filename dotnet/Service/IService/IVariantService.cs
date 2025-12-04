using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
<<<<<<< HEAD
using dotnet.Dtos;
=======
>>>>>>> user
using dotnet.Model;

namespace be_dotnet_ecommerce1.Service.IService
{
    public interface IVariantService
    {
<<<<<<< HEAD
    public Task<List<VariantFilterDTO>> getValueVariant();
        public Task<List<V_VariantFilterDTO>> getAllVariant();
        public Task<List<VariantFilterDTO>> getValueVariantByNameCategory(string? name);
=======
        public Task<List<VariantFilterDTO>> getValueVariant(int id);
>>>>>>> user
        public Task<List<Variant>> GetVariantByFilter(FilterDTO dTO);
    }
}