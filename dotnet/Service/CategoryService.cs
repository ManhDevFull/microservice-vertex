using System.Linq;
using System.Threading.Tasks;
using be.Service.IService;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
using be_dotnet_ecommerce1.Repository.IReopsitory;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Service;

namespace be_dotnet_ecommerce1.Service.IService
{
  public class CategoryService : ICategoryService
  {
    private readonly ICategoryRepository _repo;
    private readonly IProductService _productservice;
    public CategoryService(ICategoryRepository repo, IProductService productService)
    {
      _repo = repo;
      _productservice = productService;
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

    public async Task<List<V_CategoryDTO>> getAllCategory()
    {
      return await _repo.getAllCategory();
    }
    public async Task<ICollection<CategoryDTO>> getCategoriesParent()
    {
      // lấy ra sản phẩm có lượt order nhiefu nhất->lấy ra được id cate tương ướng
      var topProductByOrder = await _productservice.getTop1ProductByOrder();
      // lấy ra id cate tương ứng
      var idCate = topProductByOrder.categoryId;
      var rs = await _repo.getCateById(idCate); // lấy ra các category với idcate
      return rs;
    }

  }
}
