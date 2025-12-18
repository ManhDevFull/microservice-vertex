using Microsoft.EntityFrameworkCore;
namespace be_dotnet_ecommerce1.Model
{
  [Keyless]
  public class CategoryAdminDTO
  {
    public int id { get; set; }
    public string namecategory { get; set; } = null!;
    public int? idparent { get; set; }
    public long? product { get; set; }
  }
}