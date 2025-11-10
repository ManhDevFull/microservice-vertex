using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Dtos;
using dotnet.Dtos.admin;

namespace dotnet.Repository.IRepository
{
  public interface IProductReponsitory
  {
    public Task<PagedResult<ProductAdminDTO>> getProductAdmin(
        int page,
        int size,
        string? name,
        int? cate,
        string? brand,
        bool? stock,
        string sort = "newest");
    public int getQuantityByIdCategory(int id);
    // public Task<List<ProductFilterDTO>> getProductByFilter(FilterDTO dTO);
    public Task<List<ProductFilterDTO>> GetProductByFilter(FilterDTO dTO);
    public Task<ProductAdminDTO?> CreateProductAsync(ProductAdminCreateRequest request);
    public Task<ProductAdminDTO?> UpdateProductAsync(int productId, ProductAdminUpdateRequest request);
    public Task<bool> DeleteProductAsync(int productId);
    public Task<ProductAdminDTO?> GetProductAdminByIdAsync(int productId);
    public Task<ProductAdminDTO?> CreateVariantAsync(int productId, VariantAdminCreateRequest request);
    public Task<ProductAdminDTO?> UpdateVariantAsync(int productId, int variantId, VariantAdminUpdateRequest request);
    public Task<ProductAdminDTO?> DeleteVariantAsync(int productId, int variantId);
  }
}
