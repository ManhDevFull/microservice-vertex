
namespace dotnet.Dtos.admin
{
  public class ReviewAdminDTO
  {
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    public DateTime? CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public bool IsUpdated { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public Dictionary<string, string> VariantAttributes { get; set; } = new();
  }
}
