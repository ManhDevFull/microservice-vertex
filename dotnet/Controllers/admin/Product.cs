using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using be.Service.IService;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers.admin
{
  [Route("admin/[controller]")]
  [ApiController]
  public class ProductController : ControllerBase
  {
    private readonly IProductService _service;

    private readonly IPhotoService _photoService;
    private readonly ILogger<ProductController> _logger;
    private const int MaxProductImages = 10;
    public ProductController(IProductService service, IPhotoService photoService, ILogger<ProductController> logger)
    {
      _service = service;

      _photoService = photoService;
      _logger = logger;
    }
    [Authorize(Roles = "0")]
    [HttpPost("images/upload")]
    public async Task<IActionResult> UploadProductImages([FromForm] List<IFormFile> files)
    {
      if (files == null || files.Count == 0)
      {
        return BadRequest(new { message = "Please select at least one image to upload." });
      }

      if (files.Any(f => f == null || f.Length == 0))
      {
        return BadRequest(new { message = "One or more files are invalid or empty." });
      }

      try
      {
        var uploadTasks = files.Select(async file =>
        {
          var uploadResult = await _photoService.AddPhotoAsync(file);

          if (uploadResult.Error != null)
          {
            _logger.LogError("Cloudinary product image upload failed ({FileName}): {Message}", file.FileName, uploadResult.Error.Message);
            throw new InvalidOperationException($"Failed to upload image {file.FileName}: {uploadResult.Error.Message}");
          }

          return new
          {
            fileName = file.FileName,
            url = uploadResult.SecureUrl?.ToString(),
            publicId = uploadResult.PublicId
          };
        });

        var uploadedFiles = await Task.WhenAll(uploadTasks);

        return Ok(new
        {
          message = "Product images uploaded successfully.",
          files = uploadedFiles
        });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogWarning(ex, "Product image upload failed due to invalid data.");
        return BadRequest(new { message = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Server error while uploading product images.");
        return StatusCode(500, new { message = "Server error while uploading product images." });
      }
    }
    [HttpGet]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> GetProductAdmin([FromQuery] AdminProductFilter query)
    {
      int page = query.Page ?? 1;
      int size = query.Size ?? 30;
      string? name = query.name;
      int? cate = query.cate;
      string? brand = query.brand;
      bool? stock = query.stock;
      string sort = (query.sort ?? "newest").ToLowerInvariant();
      sort = sort switch
      {
        "priceasc" => "price_asc",
        "pricedesc" => "price_desc",
        "newest" => "newest",
        _ => "newest"
      };

      var result = await _service.getProductAdmin(page, size, name, cate, brand, stock, sort);
      return Ok(new
      {
        status = 200,
        data = result,
        message = "Success"
      });
    }
    [HttpPost]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> CreateProduct([FromForm] ProductAdminCreateForm form)
    {
      if (form == null)
      {
        return BadRequest(new { status = 400, message = "Invalid product payload." });
      }

      try
      {
        var (imageUrls, errorResult) = await PrepareProductImagesAsync(
          form.ExistingImageUrls,
          form.NewImages,
          "create product");

        if (errorResult != null)
        {
          return errorResult;
        }

        var request = new ProductAdminCreateRequest
        {
          name = form.Name,
          brandId = form.BrandId,
          categoryId = form.CategoryId,
          description = form.Description ?? string.Empty,
          imageUrls = imageUrls
        };

        var created = await _service.CreateProductAsync(request);
        return StatusCode(StatusCodes.Status201Created, new
        {
          status = 201,
          data = created,
          message = "Created"
        });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
    }

    [HttpPut("{productId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> UpdateProduct(int productId, [FromForm] ProductAdminUpdateForm form)
    {
      if (form == null)
      {
        return BadRequest(new { status = 400, message = "Invalid product payload." });
      }

      try
      {
        var original = await _service.GetProductAdminByIdAsync(productId);
        if (original == null)
        {
          return NotFound(new { status = 404, message = "Product not found" });
        }

        var (imageUrls, errorResult) = await PrepareProductImagesAsync(
          form.ExistingImageUrls,
          form.NewImages,
          "update product");

        if (errorResult != null)
        {
          return errorResult;
        }

        var request = new ProductAdminUpdateRequest
        {
          name = form.Name,
          brandId = form.BrandId,
          categoryId = form.CategoryId,
          description = form.Description
        };

        if (form.ImageUpdate || form.NewImages?.Count > 0 || form.ExistingImageUrls != null)
        {
          request.imageUrls = imageUrls;
        }

        var updated = await _service.UpdateProductAsync(productId, request);
        if (updated == null)
        {
          return NotFound(new { status = 404, message = "Product not found" });
        }

        if (request.imageUrls != null)
        {
          var removedImages = (original.imageurls ?? Array.Empty<string>())
            .Where(url => !imageUrls.Contains(url, StringComparer.OrdinalIgnoreCase))
            .ToList();

          await CleanupRemovedImagesAsync(removedImages);
        }

        return Ok(new { status = 200, data = updated, message = "Updated" });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
    }

    [HttpDelete("{productId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
      var deleted = await _service.DeleteProductAsync(productId);
      if (!deleted)
      {
        return NotFound(new { status = 404, message = "Product not found" });
      }

      return Ok(new { status = 200, message = "Deleted" });
    }

    [HttpPost("{productId:int}/variant")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> CreateVariant(int productId, [FromBody] VariantAdminCreateRequest request)
    {
      try
      {
        var updated = await _service.CreateVariantAsync(productId, request);
        if (updated == null)
        {
          return NotFound(new { status = 404, message = "Product not found" });
        }
        return Ok(new { status = 200, data = updated, message = "Variant created" });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
    }

    [HttpPut("{productId:int}/variant/{variantId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> UpdateVariant(int productId, int variantId, [FromBody] VariantAdminUpdateRequest request)
    {
      try
      {
        var updated = await _service.UpdateVariantAsync(productId, variantId, request);
        if (updated == null)
        {
          return NotFound(new { status = 404, message = "Variant not found" });
        }
        return Ok(new { status = 200, data = updated, message = "Variant updated" });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { status = 400, message = ex.Message });
      }
    }

    [HttpDelete("{productId:int}/variant/{variantId:int}")]
    [Authorize(Roles = "0")]
    public async Task<IActionResult> DeleteVariant(int productId, int variantId)
    {
      var updated = await _service.DeleteVariantAsync(productId, variantId);
      if (updated == null)
      {
        return NotFound(new { status = 404, message = "Variant not found" });
      }
      return Ok(new { status = 200, data = updated, message = "Variant deleted" });
    }
    // [HttpDelete]
    // [Authorize(Roles = "0")]
    // public async Task<IActionResult> DeleteProductAdmin([FromBody] RequestDel req)
    // {
    //   return Ok(new
    //   {
    //     status = 200,
    //     message = "Success"
    //   });
    // }

    private async Task<(List<string> ImageUrls, IActionResult? ErrorResult)> PrepareProductImagesAsync(
      IEnumerable<string>? existingImageUrls,
      IEnumerable<IFormFile>? newImages,
      string contextLabel)
    {
      var finalUrls = new List<string>();
      if (existingImageUrls != null)
      {
        foreach (var url in existingImageUrls)
        {
          if (string.IsNullOrWhiteSpace(url)) continue;
          if (!finalUrls.Any(existing => string.Equals(existing, url, StringComparison.OrdinalIgnoreCase)))
          {
            finalUrls.Add(url);
          }
        }
      }

      if (finalUrls.Count > MaxProductImages)
      {
        return (finalUrls, BadRequest(new
        {
          status = 400,
          message = $"A product can have at most {MaxProductImages} images."
        }));
      }

      var uploaded = new List<(string? PublicId, string Url)>();
      var files = newImages ?? Enumerable.Empty<IFormFile>();

      foreach (var file in files)
      {
        if (file == null || file.Length == 0)
        {
          await RollbackUploadsAsync(uploaded);
          return (finalUrls, BadRequest(new
          {
            status = 400,
            message = "One or more images are invalid or empty."
          }));
        }

        if (finalUrls.Count >= MaxProductImages)
        {
          await RollbackUploadsAsync(uploaded);
          return (finalUrls, BadRequest(new
          {
            status = 400,
            message = $"A product can have at most {MaxProductImages} images."
          }));
        }

        try
        {
          var uploadResult = await _photoService.AddPhotoAsync(file);
          if (uploadResult.Error != null || uploadResult.SecureUrl == null)
          {
            await RollbackUploadsAsync(uploaded);
            _logger.LogError(
              "Failed to upload product image ({FileName}) during {Context}: {Message}",
              file.FileName,
              contextLabel,
              uploadResult.Error?.Message ?? "unknown error");

            return (finalUrls, StatusCode(StatusCodes.Status500InternalServerError, new
            {
              message = "Failed to upload product images."
            }));
          }

          var secureUrl = uploadResult.SecureUrl.ToString();
          finalUrls.Add(secureUrl);
          uploaded.Add((uploadResult.PublicId, secureUrl));
        }
        catch (Exception ex)
        {
          await RollbackUploadsAsync(uploaded);
          _logger.LogError(ex, "Server error while uploading product image {FileName} during {Context}", file.FileName, contextLabel);
          return (finalUrls, StatusCode(StatusCodes.Status500InternalServerError, new
          {
            message = "Server error while uploading product images."
          }));
        }
      }

      return (finalUrls, null);
    }

    private async Task RollbackUploadsAsync(IEnumerable<(string? PublicId, string Url)> uploaded)
    {
      foreach (var item in uploaded)
      {
        if (string.IsNullOrWhiteSpace(item.PublicId)) continue;
        try
        {
          await _photoService.DeletePhotoAsync(item.PublicId);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to rollback product image {PublicId}", item.PublicId);
        }
      }
    }

    private async Task CleanupRemovedImagesAsync(IEnumerable<string> removedImageUrls)
    {
      foreach (var imageUrl in removedImageUrls)
      {
        if (string.IsNullOrWhiteSpace(imageUrl)) continue;

        var publicId = TryExtractCloudinaryPublicId(imageUrl);
        if (string.IsNullOrWhiteSpace(publicId)) continue;

        try
        {
          await _photoService.DeletePhotoAsync(publicId);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", publicId);
        }
      }
    }

    private static string? TryExtractCloudinaryPublicId(string imageUrl)
    {
      if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)) return null;
      if (!uri.Host.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)) return null;

      var path = uri.AbsolutePath;
      const string marker = "/upload/";
      var markerIndex = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      if (markerIndex < 0) return null;

      var relativePath = path[(markerIndex + marker.Length)..];
      if (string.IsNullOrWhiteSpace(relativePath)) return null;

      var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
      if (segments.Length == 0) return null;

      var lastSegment = segments[^1];
      var dotIndex = lastSegment.LastIndexOf('.');
      if (dotIndex > 0)
      {
        segments[^1] = lastSegment[..dotIndex];
      }

      return string.Join("/", segments);
    }
  }
}
