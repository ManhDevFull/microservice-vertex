namespace dotnet.Model
{
    public class CategoryBrandStats
    {
        public int category_id { get; set; }
        public long brand_id { get; set; }
        public long product_count { get; set; }
        public long variant_count { get; set; }
        public long units_sold { get; set; }
        public decimal revenue { get; set; }
        public DateTime updated_at { get; set; }
    }
}
