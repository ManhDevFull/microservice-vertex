using System.ComponentModel.DataAnnotations;

namespace dotnet.Dtos
{
    public class AddressCreateDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập loại địa chỉ (Nhà riêng/Cơ quan)")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        public string NameRecipient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Tel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        public int CodeWard { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết")]
        public string Detail { get; set; } = string.Empty; 

        public string? Description { get; set; } 
    }
}