using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet.Dtos.admin
{
  public class PagedResult<T>
  {
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public long Total { get; set; }            // tổng số dòng
    public int Page { get; set; }              // trang hiện tại
    public int Size { get; set; }              // số phần tử mỗi trang
  }
}