using System.Threading.Tasks;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
using dotnet.Dtos;
using dotnet.Dtos.admin;

namespace be_dotnet_ecommerce1.Service.IService
{
  public interface ICategoryService
  {
<<<<<<< HEAD
=======
    public List<CategoryDTO> getCategoryParentById(int? id);
>>>>>>> user
    public List<CategoryAdminDTO> getCategoryAdmin();
    public List<BrandOptionDTO> getBrandByCate(int? categoryId);
    public Task<CategoryAdminDTO> CreateCategoryAsync(CategoryCreateRequest request);
    public Task<CategoryAdminDTO?> UpdateCategoryAsync(int categoryId, CategoryUpdateRequest request);
    public Task<bool> DeleteCategoryAsync(int categoryId);
<<<<<<< HEAD
    public Task<List<V_CategoryDTO>> getAllCategory();
    public Task<ICollection<CategoryDTO>> getCategoriesParent();
=======
>>>>>>> user
  }
}
