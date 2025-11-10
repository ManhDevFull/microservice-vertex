
using dotnet.Dtos;
using dotnet.Repository.IRepository;
using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Service.IService;
using be.Service.IService;
using dotnet.Dtos.admin;

namespace dotnet.Service
{
  public class ProductService : IProductService
  {
    private readonly IProductReponsitory _repo;
    public ProductService(IProductReponsitory repo)
    {
      _repo = repo;
    }
    public async Task<PagedResult<ProductAdminDTO>> getProductAdmin(
        int page,
        int size,
        string? name,
        int? cate,
        string? brand,
        bool? stock,
        string sort = "newest")
    {
      var paged = await _repo.getProductAdmin(page, size, name, cate, brand, stock, sort);
      return paged;
    }

    public async Task<List<ProductFilterDTO>> getProductByFilter(FilterDTO dTO)
    {
      var result = await _repo.GetProductByFilter(dTO);
      return result;
    }


    public int getQuantityByIdCategory(int id)
    {
      var quantity = _repo.getQuantityByIdCategory(id);
      return quantity;
    }

    public Task<ProductAdminDTO?> CreateProductAsync(ProductAdminCreateRequest request)
    {
      return _repo.CreateProductAsync(request);
    }

    public Task<ProductAdminDTO?> UpdateProductAsync(int productId, ProductAdminUpdateRequest request)
    {
      return _repo.UpdateProductAsync(productId, request);
    }

    public Task<bool> DeleteProductAsync(int productId)
    {
      return _repo.DeleteProductAsync(productId);
    }

    public Task<ProductAdminDTO?> GetProductAdminByIdAsync(int productId)
    {
      return _repo.GetProductAdminByIdAsync(productId);
    }

    public Task<ProductAdminDTO?> CreateVariantAsync(int productId, VariantAdminCreateRequest request)
    {
      return _repo.CreateVariantAsync(productId, request);
    }

    public Task<ProductAdminDTO?> UpdateVariantAsync(int productId, int variantId, VariantAdminUpdateRequest request)
    {
      return _repo.UpdateVariantAsync(productId, variantId, request);
    }

    public Task<ProductAdminDTO?> DeleteVariantAsync(int productId, int variantId)
    {
      return _repo.DeleteVariantAsync(productId, variantId);
    }
  }
}
