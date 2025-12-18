using System;
using System.Collections.Generic;

namespace dotnet.Dtos.admin
{
  public class ProductSnapshotDTO
  {
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public IReadOnlyList<string> Gallery { get; set; } = Array.Empty<string>();
    public int VariantId { get; set; }
    public Dictionary<string, string> VariantAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
  }
}
