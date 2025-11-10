using System.Threading.Tasks;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
using dotnet.Dtos.admin;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Repository.IReopsitory
{
    public interface ICategoryRepository
    {
        public List<Category> getParentById(int? id);
        public List<CategoryAdminDTO> getCategoryAdmin();
        public Task<CategoryAdminDTO?> GetCategoryByIdAsync(int categoryId);
        public Task<CategoryAdminDTO> CreateCategoryAsync(CategoryCreateRequest request);
        public Task<CategoryAdminDTO?> UpdateCategoryAsync(int categoryId, CategoryUpdateRequest request);
        public Task<bool> DeleteCategoryAsync(int categoryId);
        public List<BrandOptionDTO> getBrandByCate(int? categoryId);
    }
}
