namespace be_dotnet_ecommerce1.Dtos
{
    public class UserProfileDTO
    {
        public int Id { get; set; }
        public string? Email { get; set; } 
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}