using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Model;
using be_dotnet_ecommerce1.Repository.IReopsitory;
using dotnet.Dtos.admin;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;

namespace be_dotnet_ecommerce1.Repository
{

  public class CategoryRepository : ICategoryRepository
  {
    private const string CategoryAdminSql = @"
                WITH RECURSIVE descendants AS (
                  SELECT id AS root_id, id
                  FROM category
                  UNION ALL
                  SELECT d.root_id, c.id
                  FROM category c
                  JOIN descendants d ON c.parent_id = d.id
                )
                SELECT
                  cat.id,
                  cat.namecategory,
                  cat.parent_id AS idparent,
                  COUNT(DISTINCT p.id) AS product
                FROM category AS cat
                LEFT JOIN descendants d ON d.root_id = cat.id
                LEFT JOIN product p ON p.category = d.id
                GROUP BY cat.id, cat.namecategory, cat.parent_id
                ORDER BY cat.id";

    private readonly ConnectData _connect;
    public CategoryRepository(ConnectData connect)
    {
      _connect = connect;
    }
    public List<Category> getParentById(int? id)
    {
      return _connect.categories.Where(c => c.idparent == id).ToList();
    }
    public List<CategoryAdminDTO> getCategoryAdmin()
    {
      return _connect.categoryAdmins.FromSqlRaw(CategoryAdminSql).AsNoTracking().ToList();
    }

    public async Task<CategoryAdminDTO?> GetCategoryByIdAsync(int categoryId)
    {
      var sql = $@"
                WITH category_cte AS (
                  {CategoryAdminSql}
                )
                SELECT *
                FROM category_cte
                WHERE id = {{0}}";

      return await _connect.categoryAdmins
        .FromSqlRaw(sql, categoryId)
        .AsNoTracking()
        .FirstOrDefaultAsync();
    }

    public async Task<CategoryAdminDTO> CreateCategoryAsync(CategoryCreateRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Name))
        throw new ArgumentException("Category name is required.", nameof(request.Name));

      var trimmedName = request.Name.Trim();
      int? parentId = request.ParentId;

      if (parentId.HasValue)
      {
        var parentExists = await _connect.categories.AnyAsync(c => c.id == parentId.Value);
        if (!parentExists)
        {
          throw new ArgumentException("Parent category not found.", nameof(request.ParentId));
        }
      }

      var nameConflict = await _connect.categories
        .AnyAsync(c =>
          c.idparent == parentId &&
          EF.Functions.ILike(c.namecategory, trimmedName));

      if (nameConflict)
        throw new InvalidOperationException("A category with the same name already exists under the selected parent.");

      var entity = new Category
      {
        namecategory = trimmedName,
        idparent = parentId
      };

      _connect.categories.Add(entity);
      await _connect.SaveChangesAsync();

      var result = await GetCategoryByIdAsync(entity.id);
      if (result == null)
      {
        throw new InvalidOperationException("Failed to load created category.");
      }

      return result;
    }

    public async Task<CategoryAdminDTO?> UpdateCategoryAsync(int categoryId, CategoryUpdateRequest request)
    {
      var category = await _connect.categories.FirstOrDefaultAsync(c => c.id == categoryId);
      if (category == null)
      {
        return null;
      }

      bool hasChanges = false;

      if (request.Name is not null)
      {
        var trimmed = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
          throw new ArgumentException("Category name is required.", nameof(request.Name));

        if (!string.Equals(category.namecategory, trimmed, StringComparison.OrdinalIgnoreCase))
        {
          var targetParentId = request.ParentIdSpecified ? request.ParentId : category.idparent;
          var conflict = await _connect.categories
            .AnyAsync(c =>
              c.id != categoryId &&
              c.idparent == targetParentId &&
              EF.Functions.ILike(c.namecategory, trimmed));
          if (conflict)
            throw new InvalidOperationException("Another category with the same name already exists under the selected parent.");

            category.namecategory = trimmed;
            hasChanges = true;
        }
      }

      if (request.ParentIdSpecified)
      {
        var newParentId = request.ParentId;

        if (newParentId == categoryId)
          throw new InvalidOperationException("A category cannot be its own parent.");

        if (newParentId.HasValue)
        {
          var parentExists = await _connect.categories.AnyAsync(c => c.id == newParentId.Value);
          if (!parentExists)
            throw new ArgumentException("Parent category not found.", nameof(request.ParentId));

          if (await IsDescendantAsync(categoryId, newParentId.Value))
            throw new InvalidOperationException("Cannot assign a descendant category as parent.");
        }

        if (category.idparent != newParentId)
        {
          category.idparent = newParentId;
          hasChanges = true;
        }
      }

      if (!hasChanges)
      {
        return await GetCategoryByIdAsync(categoryId);
      }

      await _connect.SaveChangesAsync();
      return await GetCategoryByIdAsync(categoryId);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
      var category = await _connect.categories.FirstOrDefaultAsync(c => c.id == categoryId);
      if (category == null)
      {
        return false;
      }

      var hasChildren = await _connect.categories.AnyAsync(c => c.idparent == categoryId);
      if (hasChildren)
        throw new InvalidOperationException("Cannot delete a category that has child categories.");

      var hasProducts = await _connect.products.AnyAsync(p => p.categoryId == categoryId);
      if (hasProducts)
        throw new InvalidOperationException("Cannot delete a category that contains products.");

      _connect.categories.Remove(category);
      await _connect.SaveChangesAsync();
      return true;
    }

    public List<BrandOptionDTO> getBrandByCate(int? categoryId)
    {
      if (categoryId.HasValue)
      {
        return (from stats in _connect.category_brand_stats.AsNoTracking()
                join brand in _connect.brands.AsNoTracking() on stats.brand_id equals brand.id
                where stats.category_id == categoryId.Value
                orderby brand.name
                select new BrandOptionDTO
                {
                  id = brand.id,
                  name = brand.name
                }).ToList();
      }

      return _connect.brands
        .AsNoTracking()
        .OrderBy(b => b.name)
        .Select(b => new BrandOptionDTO
        {
          id = b.id,
          name = b.name
        })
        .ToList();
    }

    private async Task<bool> IsDescendantAsync(int categoryId, int potentialParentId)
    {
      var allCategories = await _connect.categories
        .AsNoTracking()
        .Select(c => new { c.id, c.idparent })
        .ToListAsync();

      var lookup = allCategories.ToLookup(c => c.idparent, c => c.id);

      var directChildren = lookup[categoryId];
      if (!directChildren.Any())
        return false;

      var queue = new Queue<int>(directChildren);
      var visited = new HashSet<int>(queue);
      while (queue.Count > 0)
      {
        var current = queue.Dequeue();
        if (current == potentialParentId)
        {
          return true;
        }

        foreach (var child in lookup[current])
        {
          if (visited.Add(child))
            queue.Enqueue(child);
        }
      }

      return false;
    }
  }
}
