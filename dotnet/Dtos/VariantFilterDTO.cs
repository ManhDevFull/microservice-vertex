namespace be_dotnet_ecommerce1.Dtos
{
    public class VariantFilterDTO
    {
        public int id {get; set;}
        public string? namecategory {get; set;}
        public string[]? brand{get; set;}
        public Dictionary<string, string[]>? variant {get; set;}
    }
}