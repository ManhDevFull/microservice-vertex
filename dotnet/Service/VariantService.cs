using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Repository.IRepository;
using be_dotnet_ecommerce1.Service.IService;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Service
{
    public class VariantService : IVariantService
    {
        private readonly IVariantRepository _repo;
        public VariantService(IVariantRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<VariantFilterDTO>> getValueVariant()
        {
            return await _repo.GetValueVariant();
        }
        public async Task<List<V_VariantFilterDTO>> getAllVariant()
        {
            return await _repo.getAllVariant();
        }
        public async Task<List<Variant>> GetVariantByFilter(FilterDTO dTO)
        {
            // return await _repo.GetVariantByFilter(dTO);
            return null;
        }
        public async Task<List<VariantFilterDTO>> getValueVariantByNameCategory(string? name)
        {
            var rs = await _repo.GetValueVariantByNameCategory(name);
            return rs;
        }
    }
}