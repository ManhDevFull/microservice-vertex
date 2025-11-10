using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet.Model
{
  public class Discount
  {
    public int id { get; set; }
    public int? typediscount { get; set; }
    public int? discount { get; set; }
    public DateTime starttime { get; set; }
    public DateTime endtime { get; set; }
    public DateTime? createtime { get; set; }
    public List<DiscountProduct>? discountProducts { get; set; }
  }
}