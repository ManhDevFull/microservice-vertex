<<<<<<< HEAD

using dotnet.Model;

namespace dotnet.Dtos
=======
using dotnet.Model;

namespace be_dotnet_ecommerce1.Dtos
>>>>>>> user
{
    public class ProductFilterDTO
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? brand { get; set; }
        public int categoryId { get; set; }
        public string? categoryName { get; set; }
        public List<string>? imgUrls { get; set; }
        //public int totalStock { get; set; }
<<<<<<< HEAD
        public List<VariantDTO>? variant { get; set; }
=======
        public VariantDTO[]? variant { get; set; }
        //public DateTime updateDate { get; set; }
        public Discount[]? discount { get; set; } 
>>>>>>> user
        public int rating { get; set; }
        public int order { get; set; }
    }
}