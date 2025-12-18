using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Dtos.admin
{
  public abstract class ProductAdminImageFormBase
  {
    [FromForm(Name = "newImages")]
    public List<IFormFile> NewImages { get; set; } = new();

    [FromForm(Name = "existingImageUrls")]
    public List<string> ExistingImageUrls { get; set; } = new();

    [FromForm(Name = "imageUpdate")]
    public bool ImageUpdate { get; set; }
  }

  public class ProductAdminCreateForm : ProductAdminImageFormBase
  {
    [FromForm(Name = "name")]
    public string Name { get; set; } = null!;

    [FromForm(Name = "brandId")]
    public long BrandId { get; set; }

    [FromForm(Name = "categoryId")]
    public int CategoryId { get; set; }

    [FromForm(Name = "description")]
    public string Description { get; set; } = string.Empty;
  }

  public class ProductAdminUpdateForm : ProductAdminImageFormBase
  {
    [FromForm(Name = "name")]
    public string? Name { get; set; }

    [FromForm(Name = "brandId")]
    public long? BrandId { get; set; }

    [FromForm(Name = "categoryId")]
    public int? CategoryId { get; set; }

    [FromForm(Name = "description")]
    public string? Description { get; set; }
  }
}
