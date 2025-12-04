<<<<<<< HEAD


namespace dotnet.Dtos
=======
namespace be_dotnet_ecommerce1.Dtos
>>>>>>> user
{
    public class VariantDTO
    {
        public int id { get; set; }
<<<<<<< HEAD
        public Dictionary<string, string>? valuevariant { get; set; }
        public int stock { get; set; }
        public List<DiscountDTO>? discounts { get; set; }
        public int price { get; set; }
=======
        public string valuevariant { get; set; } = null!; // JSONB
        public int stock { get; set; }
        public int inputprice { get; set; }
        public int price { get; set; }
        public DateTime createdate { get; set; }
        public DateTime updatedate { get; set; }
>>>>>>> user
    }
}