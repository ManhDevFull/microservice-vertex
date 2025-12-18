using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet.Dtos.admin
{
  public class AdminProductFilter
  {
    public string? name { get; set; }
    public int? cate { get; set; }
    public string? brand { get; set; }
    public bool? stock { get; set; }
    public string? sort { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; }
  }
  public class RequestDel{
    public int id { get; set; }
  }
}