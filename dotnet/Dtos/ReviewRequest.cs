using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace be_dotnet_ecommerce1.Dtos
{
    public class CreateReviewRequest
    {
        [Required]
        public int OrderDetailId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // 1 đến 5 sao

        public string? Content { get; set; }

        // Nhận danh sách file ảnh từ form frontend
        public List<IFormFile>? Images { get; set; }
    }

    public class UpdateReviewRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Content { get; set; }

        // Ảnh mới (nếu gửi kèm sẽ thay thế danh sách ảnh cũ)
        public List<IFormFile>? Images { get; set; }
    }
}
