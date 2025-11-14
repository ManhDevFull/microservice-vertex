namespace be_dotnet_ecommerce1.Dtos
{
    public class VariantFilterDTO
    {
        // public int? id { get; set; }
        // public Dictionary<string, string[]>? valuevariant { get; set; }
        public string key { get; set; } = null!;
        public string[]? values { get; set; }

    }
}