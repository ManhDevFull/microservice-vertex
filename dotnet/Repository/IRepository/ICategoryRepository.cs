using System.Threading.Tasks;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
<<<<<<< HEAD
using dotnet.Dtos;
=======
>>>>>>> user
using dotnet.Dtos.admin;
using dotnet.Model;

namespace be_dotnet_ecommerce1.Repository.IReopsitory
{
    public interface ICategoryRepository
    {
<<<<<<< HEAD
=======
        public List<Category> getParentById(int? id);
>>>>>>> user
        public List<CategoryAdminDTO> getCategoryAdmin();
        public Task<CategoryAdminDTO?> GetCategoryByIdAsync(int categoryId);
        public Task<CategoryAdminDTO> CreateCategoryAsync(CategoryCreateRequest request);
        public Task<CategoryAdminDTO?> UpdateCategoryAsync(int categoryId, CategoryUpdateRequest request);
        public Task<bool> DeleteCategoryAsync(int categoryId);
        public List<BrandOptionDTO> getBrandByCate(int? categoryId);
<<<<<<< HEAD
        public Task<List<V_CategoryDTO>> getAllCategory();
        public Task<ICollection<CategoryDTO>> getCateById(int id);
=======
>>>>>>> user
    }
}
