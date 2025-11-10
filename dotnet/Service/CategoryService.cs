using System.Linq;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
using be_dotnet_ecommerce1.Repository.IReopsitory;
using dotnet.Dtos.admin;

namespace be_dotnet_ecommerce1.Service.IService
{
  public class CategoryService : ICategoryService
  {
    private readonly ICategoryRepository _repo;
    public CategoryService(ICategoryRepository repo)
    {
      _repo = repo;
    }
    public List<CategoryDTO> getCategoryParentById(int? id)
    {
      var list = _repo.getParentById(id).Select(c => new CategoryDTO
      {
        _id = c.id,
        name_category = c.namecategory
      }).ToList();
      return list;
    }
    public List<CategoryAdminDTO> getCategoryAdmin()
    {
      var list = _repo.getCategoryAdmin();
      return list;
    }
    public List<BrandOptionDTO> getBrandByCate(int? categoryId)
    {
      var list = _repo.getBrandByCate(categoryId);
      return list;
    }
    public Task<CategoryAdminDTO> CreateCategoryAsync(CategoryCreateRequest request)
    {
      return _repo.CreateCategoryAsync(request);
    }
    public Task<CategoryAdminDTO?> UpdateCategoryAsync(int categoryId, CategoryUpdateRequest request)
    {
      return _repo.UpdateCategoryAsync(categoryId, request);
    }
    public Task<bool> DeleteCategoryAsync(int categoryId)
    {
      return _repo.DeleteCategoryAsync(categoryId);
    }
  }
}
