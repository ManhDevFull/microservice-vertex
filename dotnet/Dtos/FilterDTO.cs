namespace be_dotnet_ecommerce1.Controllers
{
    public class FilterDTO
    {
        public Dictionary<string, string[]>? Filter { get; set; } 
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? query { get; set; }
    }
}