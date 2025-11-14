namespace be_dotnet_ecommerce1.Dtos
{
    public class PagedResultDTO<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; } // tổng số lượng sản phẩm
        public int TotalPage { get; set; } // tổng số trang
    }
}