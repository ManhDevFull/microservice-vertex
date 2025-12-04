namespace be_dotnet_ecommerce1.Dtos
{
    public class VariantFilterDTO
    {
<<<<<<< HEAD
        public int id {get; set;}
        public string? namecategory {get; set;}
        public string[]? brand{get; set;}
        public Dictionary<string, string[]>? variant {get; set;}
=======
        // public int? id { get; set; }
        // public Dictionary<string, string[]>? valuevariant { get; set; }
        public string key { get; set; } = null!;
        public string[]? values { get; set; }

>>>>>>> user
    }
}