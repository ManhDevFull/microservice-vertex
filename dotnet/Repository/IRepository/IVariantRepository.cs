using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
<<<<<<< HEAD
using dotnet.Dtos;
=======
>>>>>>> user
using dotnet.Model;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public interface IVariantRepository
    {
<<<<<<< HEAD
        public Task<List<VariantFilterDTO>> GetValueVariant();
        public Task<List<VariantFilterDTO>> GetValueVariantByNameCategory(string? name);
        public Task<List<V_VariantFilterDTO>> getAllVariant();
=======
        public Task<List<VariantFilterDTO>> GetValueVariant(int id);
        public Task<List<Variant>> GetVariantByFilter(FilterDTO dTO);
>>>>>>> user
    }
}