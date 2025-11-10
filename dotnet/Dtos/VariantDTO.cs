using System.Text.Json.Serialization;

namespace be_dotnet_ecommerce1.Dtos
{
    public class VariantDTO
    {
        public int id { get; set; }
        public Dictionary<string, string>? valuevariant { get; set; }
        public int stock { get; set; }
        public List<DiscountDTO>? discounts { get; set; }
        public int price { get; set; }
    }
}