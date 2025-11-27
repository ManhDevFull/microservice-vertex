namespace dotnet.Dtos
{
    public class AddressDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string NameRecipient { get; set; } = string.Empty;
        public string Tel { get; set; } = string.Empty;
        public int CodeWard { get; set; } // Trả về mã để FE tự dịch
        public string Detail { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FullAddress => $"{Detail} (Mã: {CodeWard})"; 
    }
}