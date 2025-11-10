// File: Repository/ProductReponsitory.cs
using System.Text.Json;
using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Repository;
using be_dotnet_ecommerce1.Repository.IRepository;
using dotnet.Dtos;
using dotnet.Model;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using dotnet.Dtos.admin;

namespace dotnet.Repository
{
  public class ProductReponsitory : IProductReponsitory
  {
    private readonly ConnectData _connect;
    private VariantRepository variantRepository;
    public ProductReponsitory(ConnectData connect)
    {
      _connect = connect;
      variantRepository = new VariantRepository(_connect);
    }

   public async Task<List<ProductFilterDTO>> GetProductByFilter(FilterDTO dTO)
{
    try
    {
        // Lấy danh sách variant thỏa filter
        var variants = await variantRepository.GetVariantByFilter(dTO) ?? new List<Variant>();
        var productIds = variants.Select(v => v.productid).Distinct().ToList();

        // Lấy danh sách sản phẩm có variant thuộc list
        var products = await _connect.products
            .Include(p => p.category)
            .Include(p => p.brand) // thêm include brand
            .Where(p => productIds.Contains(p.id))
            .Select(p => new ProductFilterDTO
            {
                id = p.id,
                name = p.nameproduct,
                description = p.description,
                // ✅ lấy tên brand
                brand = p.brand != null ? p.brand.name : string.Empty,
                categoryId = p.categoryId,
                categoryName = p.category != null ? p.category.namecategory : null,
                // ✅ đổi từ string[] sang List<string>
                imgUrls = p.imageurls != null ? p.imageurls.ToList() : new List<string>(),

                // ✅ map variant
                variant = _connect.variants
                    .Where(v => v.productid == p.id)
                    .Select(v => new VariantDTO
                    {
                        id = v.id,
                        // ✅ convert JSONB -> string
                        valuevariant = v.valuevariant.RootElement.ToString(),
                        stock = v.stock,
                        inputprice = v.inputprice,
                        price = v.price,
                        createdate = v.createdate,
                        updatedate = v.updatedate
                    }).ToArray(),

                // discount
                discount = (from dp in _connect.discountProducts
                            join d in _connect.discounts on dp.discountid equals d.id
                            join v in _connect.variants on dp.variantid equals v.id
                            where v.productid == p.id
                            select d).ToArray(),

                // rating
                rating = (from r in _connect.reviews
                          join o in _connect.orders on r.orderid equals o.id
                          join v in _connect.variants on o.variantid equals v.id
                          where v.productid == p.id
                          select (int?)r.rating).Sum() ?? 0,

                // order count
                order = (from o in _connect.orders
                         join v in _connect.variants on o.variantid equals v.id
                         where v.productid == p.id
                         select o).Count()
            })
            .ToListAsync();

        return products ?? new List<ProductFilterDTO>();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
}



    public int getQuantityByIdCategory(int id)
    {
      var quantity = _connect.products.Count(p => p.categoryId == id);
      return quantity;
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
      page = Math.Max(1, page);
      size = Math.Clamp(size, 1, 100);
      var offset = (page - 1) * size;

      var q = _connect.productAdmins.AsNoTracking().AsQueryable();

      // ---- FILTER ----
      if (!string.IsNullOrWhiteSpace(name))
        q = q.Where(p => EF.Functions.ILike(p.name!, $"%{name}%")); // ILIKE cho Postgres

      if (cate.HasValue)
        q = q.Where(p => p.category_id == cate.Value);

      if (!string.IsNullOrWhiteSpace(brand))
        q = q.Where(p => p.brand == brand);

      // Lọc tồn kho: có/không có biến thể stock > 0
      if (stock.HasValue)
      {
        if (stock.Value)
        {
          q = from p in q
              where _connect.variants.Any(v => v.productid == p.product_id && v.stock > 0)
              select p;
        }
        else
        {
          q = from p in q
              where !_connect.variants.Any(v => v.productid == p.product_id && v.stock > 0)
              select p;
        }
      }

      // ---- SORT ----
      q = sort switch
      {
        "name_asc" => q.OrderBy(p => p.name),
        "name_desc" => q.OrderByDescending(p => p.name),
        "price_asc" => q.OrderBy(p => p.min_price ?? int.MaxValue),
        "price_desc" => q.OrderByDescending(p => p.min_price ?? int.MinValue),
        "oldest" => q.OrderBy(p => p.createdate ?? DateTime.MinValue),
        "updated" => q.OrderByDescending(p => p.updatedate ?? p.createdate ?? DateTime.MinValue),
        _ => q.OrderByDescending(p => p.createdate ?? DateTime.MinValue) // newest
      };

      // ---- PAGING ----
      var total = await q.CountAsync();
      var rows = await q.Skip(offset).Take(size).ToListAsync();

      return new PagedResult<ProductAdminDTO>
      {
        Items = rows,
        Total = total,
        Page = page,
        Size = size
      };
    }

    private static JsonDocument BuildJsonDocument(Dictionary<string, string> value)
    {
      var payload = value ?? new Dictionary<string, string>();
      var json = JsonSerializer.Serialize(payload);
      return JsonDocument.Parse(json);
    }

    public async Task<ProductAdminDTO?> CreateProductAsync(ProductAdminCreateRequest request)
    {
      if (request == null) throw new ArgumentNullException(nameof(request));
      if (string.IsNullOrWhiteSpace(request.name))
        throw new ArgumentException("Product name is required", nameof(request.name));

      var brandExists = await _connect.brands.AnyAsync(b => b.id == request.brandId);
      if (!brandExists)
        throw new ArgumentException("Brand not found", nameof(request.brandId));

      var categoryExists = await _connect.categories.AnyAsync(c => c.id == request.categoryId);
      if (!categoryExists)
        throw new ArgumentException("Category not found", nameof(request.categoryId));

      var now = DateTime.UtcNow;

      await using var transaction = await _connect.Database.BeginTransactionAsync();

      var product = new Product
      {
        nameproduct = request.name.Trim(),
        description = request.description?.Trim() ?? string.Empty,
        brand_id = request.brandId,
        categoryId = request.categoryId,
        imageurls = request.imageUrls?.ToArray() ?? Array.Empty<string>(),
        createdate = now,
        updatedate = now,
        isdeleted = false
      };

      _connect.products.Add(product);
      await _connect.SaveChangesAsync();

      if (request.variants != null && request.variants.Count > 0)
      {
        foreach (var variant in request.variants)
        {
          var newVariant = new Variant
          {
            productid = product.id,
            valuevariant = BuildJsonDocument(variant.valuevariant),
            stock = variant.stock,
            inputprice = variant.inputprice,
            price = variant.price,
            createdate = now,
            updatedate = now,
            isdeleted = false
          };
          _connect.variants.Add(newVariant);
        }
        await _connect.SaveChangesAsync();
      }

      await transaction.CommitAsync();
      return await GetProductAdminByIdAsync(product.id);
    }

    public async Task<ProductAdminDTO?> UpdateProductAsync(int productId, ProductAdminUpdateRequest request)
    {
      var product = await _connect.products.FirstOrDefaultAsync(p => p.id == productId && !p.isdeleted);
      if (product == null) return null;

      var now = DateTime.UtcNow;

      if (!string.IsNullOrWhiteSpace(request.name))
        product.nameproduct = request.name.Trim();

      if (!string.IsNullOrWhiteSpace(request.description))
        product.description = request.description.Trim();

      if (request.brandId.HasValue)
      {
        var brandExists = await _connect.brands.AnyAsync(b => b.id == request.brandId.Value);
        if (!brandExists)
          throw new ArgumentException("Brand not found", nameof(request.brandId));
        product.brand_id = request.brandId.Value;
      }

      if (request.categoryId.HasValue)
      {
        var categoryExists = await _connect.categories.AnyAsync(c => c.id == request.categoryId.Value);
        if (!categoryExists)
          throw new ArgumentException("Category not found", nameof(request.categoryId));
        product.categoryId = request.categoryId.Value;
      }

      if (request.imageUrls != null)
        product.imageurls = request.imageUrls.ToArray();

      product.updatedate = now;

      await _connect.SaveChangesAsync();
      return await GetProductAdminByIdAsync(productId);
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
      var product = await _connect.products
        .Include(p => p.variants)
        .FirstOrDefaultAsync(p => p.id == productId && !p.isdeleted);

      if (product == null) return false;

      var now = DateTime.UtcNow;
      product.isdeleted = true;
      product.updatedate = now;

      foreach (var variant in product.variants.Where(v => !v.isdeleted))
      {
        variant.isdeleted = true;
        variant.updatedate = now;
      }

      await _connect.SaveChangesAsync();
      return true;
    }

    public async Task<ProductAdminDTO?> GetProductAdminByIdAsync(int productId)
    {
      var sql = $"SELECT * FROM ({ConnectData.ProductAdminSql}) AS product_admin WHERE product_id = {{0}}";
      return await _connect.Set<ProductAdminDTO>()
        .FromSqlRaw(sql, productId)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    }

    public async Task<ProductAdminDTO?> CreateVariantAsync(int productId, VariantAdminCreateRequest request)
    {
      var product = await _connect.products.FirstOrDefaultAsync(p => p.id == productId && !p.isdeleted);
      if (product == null) return null;

      var now = DateTime.UtcNow;
      var variant = new Variant
      {
        productid = productId,
        valuevariant = BuildJsonDocument(request.valuevariant),
        stock = request.stock,
        inputprice = request.inputprice,
        price = request.price,
        createdate = now,
        updatedate = now,
        isdeleted = false
      };
      _connect.variants.Add(variant);
      product.updatedate = now;

      await _connect.SaveChangesAsync();
      return await GetProductAdminByIdAsync(productId);
    }

    public async Task<ProductAdminDTO?> UpdateVariantAsync(int productId, int variantId, VariantAdminUpdateRequest request)
    {
      var product = await _connect.products.FirstOrDefaultAsync(p => p.id == productId && !p.isdeleted);
      if (product == null) return null;

      var variant = await _connect.variants.FirstOrDefaultAsync(v => v.id == variantId && v.productid == productId && !v.isdeleted);
      if (variant == null) return null;

      var now = DateTime.UtcNow;

      if (request.valuevariant != null)
        variant.valuevariant = BuildJsonDocument(request.valuevariant);

      if (request.stock.HasValue)
        variant.stock = request.stock.Value;

      if (request.inputprice.HasValue)
        variant.inputprice = request.inputprice.Value;

      if (request.price.HasValue)
        variant.price = request.price.Value;

      variant.updatedate = now;
      product.updatedate = now;

      await _connect.SaveChangesAsync();
      return await GetProductAdminByIdAsync(productId);
    }

    public async Task<ProductAdminDTO?> DeleteVariantAsync(int productId, int variantId)
    {
      var product = await _connect.products.FirstOrDefaultAsync(p => p.id == productId && !p.isdeleted);
      if (product == null) return null;

      var variant = await _connect.variants.FirstOrDefaultAsync(v => v.id == variantId && v.productid == productId && !v.isdeleted);
      if (variant == null) return null;

      var now = DateTime.UtcNow;
      variant.isdeleted = true;
      variant.updatedate = now;
      product.updatedate = now;

      await _connect.SaveChangesAsync();
      return await GetProductAdminByIdAsync(productId);
    }

  }
}
