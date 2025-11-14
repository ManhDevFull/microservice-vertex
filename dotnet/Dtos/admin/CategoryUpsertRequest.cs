using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dotnet.Dtos.admin
{
  public sealed class CategoryCreateRequest
  {
    [Required]
    [MaxLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parentId")]
    public int? ParentId { get; set; }
  }

  public sealed class CategoryUpdateRequest
  {
    private int? _parentId;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("parentId")]
    public int? ParentId
    {
      get => _parentId;
      set
      {
        _parentId = value;
        ParentIdSpecified = true;
      }
    }

    [JsonIgnore]
    public bool ParentIdSpecified { get; private set; }
  }
}
