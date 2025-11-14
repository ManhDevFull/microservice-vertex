namespace be_dotnet_ecommerce1.Dtos
{
    public class VariantDTO
    {
        public int id { get; set; }
        public string valuevariant { get; set; } = null!; // JSONB
        public int stock { get; set; }
        public int inputprice { get; set; }
        public int price { get; set; }
        public DateTime createdate { get; set; }
        public DateTime updatedate { get; set; }
    }
}