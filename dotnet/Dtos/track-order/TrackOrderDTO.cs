using Microsoft.EntityFrameworkCore;

namespace dotnet.Dtos.track_order
{
    [Keyless] // coi nó như 1 cái view, không có khóa chính chỉ dùng để đọc dữ liệu (map dữ liệu khi query dbcontex vào DTO)
    public class TrackOrderDTO
    {
        // public int idOrder { get; set; }
        // public int idAccount {get; set;}
        // public int idProduct {get; set;}
        // public string? status {get; set;}
        // public string? nameProduct { get; set; }
        // public string? description { get; set; }
        // public string[]? imgUrls {get; set;}
        // public int price { get; set; }
        // public int quantity { get; set; }
        // public int subtotal { get; set; }
        public int idOrder { get; set; }
        public int idAccount {get; set;}
        public int totalPrice {get; set;}
        public string? status {get; set;}
        public DateTime sendorder{get;set;}
    }
}