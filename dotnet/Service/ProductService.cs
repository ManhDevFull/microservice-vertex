
using dotnet.Dtos;
using dotnet.Repository.IRepository;
using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Service.IService;
using be.Service.IService;
using dotnet.Dtos.admin;
using be_dotnet_ecommerce1.Repository.IReopsitory;

namespace dotnet.Service
{
  public class ProductService : IProductService
  {
    private readonly IProductReponsitory _repo;
    private readonly ICategoryRepository _categoryRepository;
    public ProductService(IProductReponsitory repo, ICategoryRepository categoryRepository)
    {
      _repo = repo;
      _categoryRepository = categoryRepository;
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

    public async Task<PagedResultDTO<ProductFilterDTO>> getProductByFilter(FilterDTO dTO)
    {
      var conditions = new List<string>();
      var baseSql = @"FROM v_products_filter view";
      if (dTO.Filter != null)
      {
        foreach (var item in dTO.Filter)
        {
          var key = item.Key;
          if (key == "price")
          {
            if (item.Value.Count() == 2)
            {
              var min = item.Value[0];
              var max = item.Value[1];
              //conditions.Add($"v.price BETWEEN {min} AND {max}");
              conditions.Add($@"exists (
                select 1 
                from jsonb_array_elements(view.variant) as elem
                WHERE (elem->'price')::numeric BETWEEN {min} AND {max}
              )");
            }
          }
          else
          {
            var values = string.Join(",", item.Value.Select(v => $"'{v}'"));
            if (key == "brand")
              conditions.Add($"view.brand IN ({values})");
            else if (key == "namecategory")
              conditions.Add($"view.categoryName  IN ({values})");
            else
              //conditions.Add($"v.valuevariant ->> '{key}' IN ({values})");
              conditions.Add($@" exists (
                select 1 
                from jsonb_array_elements(view.variant) as elem
                WHERE ((elem->'valuevariant')->>'{key}') IN ({values})
            )");
          }
        }
      }
      // nối where
      if (dTO.query != "")
        conditions.Add($"name ILIKE '%{dTO.query}%'");
      string wheresql = "";
      if (conditions.Any())
        wheresql = " where " + string.Join(" and ", conditions);
      // đếm số lượng sản phẩm
      var sqlcountProduct = $"select count(distinct view.id) as \"Value\" {baseSql} {wheresql}"; // cần phải có tên cột là value
      // lấy sản phẩm 
      var sqlData = $@"select distinct view.*
      {baseSql}
      {wheresql}
      order by view.id
      offset {(dTO.pageNumber - 1) * dTO.pageSize} rows 
      fetch next  {dTO.pageSize} rows only"; // rowns only

      // thực thi sql
      var totalCount = await _repo.countProductBySql(sqlcountProduct); // đếm số lương sản phẩm

      var products = await _repo.getProductBySql(sqlData); // lấy sản phẩm bằng sql

      // nếu không có sản phẩm nào
      if (totalCount == 0 || !products.Any())
      {
        return new PagedResultDTO<ProductFilterDTO>
        {
          Items = new List<ProductFilterDTO>(),
          TotalCount = 0,
          TotalPage = 0,
          PageNumber = dTO.pageNumber,
          PageSize = dTO.pageSize
        };
      }

      var totalPage = (int)Math.Ceiling(totalCount / (double)dTO.pageSize);
      return new PagedResultDTO<ProductFilterDTO>
      {
        Items = products,
        TotalCount = totalCount,
        TotalPage = totalPage,
        PageNumber = dTO.pageNumber,
        PageSize = dTO.pageSize
      };
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
    // lấy ra 4 sản phẩm có giảm giá
    public async Task<ICollection<ProductFilterDTO>> getProductsHaveDiscount()
    {
      var sql = @"SELECT *
        FROM v_products_filter v
        WHERE EXISTS (
            SELECT 1
            FROM jsonb_array_elements(v.variant) AS var(variant)
            JOIN LATERAL jsonb_array_elements(var.variant->'discounts') AS d(discount) ON TRUE
            -- WHERE (d.discount->>'endtime')::timestamp > NOW()
        )
        LIMIT 4;";
      var rs = await _repo.getProductBySql(sql);
      return rs;
    }
    // lấy ra sản phẩm có lượng mua nhiều nhất
    public async Task<ProductFilterDTO> getTop1ProductByOrder()
    {
      var sql = @"
              SELECT *
              FROM v_products_filter
              ORDER BY ""order"" DESC
              LIMIT 1
          ";
      var rs = await _repo.getProductBySql(sql);
      return rs.FirstOrDefault();
    }
    // lấy ra các sản phẩm liên quan với sản phẩm có lượng mua nhiều nhât
    // lấy theo idparent của category
    public async Task<ICollection<ProductFilterDTO>> getProductFrequently()
    {
      var topProductByOrder = await getTop1ProductByOrder(); // lấy sản phẩm có lượng mua nhiều nhất
      // lấy ra các category con (category có idparent  = topProductByOrder.categoryId)
      var categories = await _categoryRepository.getCateById(topProductByOrder.categoryId);
      // lấy ra danh sách id category con 
      var idcate = categories.Select(c => c._id).ToList();
      // chuển thành chuỗi
      var inClause = string.Join(",", idcate);
      var sql = $@"
          SELECT * from v_products_filter
          WHERE categoryid IN ({inClause})
          LIMIT 8
        ";
      var products = await _repo.getProductBySql(sql);
      return products;
    }
  }

}
