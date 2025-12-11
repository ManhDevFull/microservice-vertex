using Microsoft.EntityFrameworkCore;

namespace dotnet.Dtos.track_order
{
    [Keyless]
    public class TimeLineDTO
    {
        public int idOrder {get; set;}
        public DateTime orderdate {get; set;}
        public DateTime receivedate {get; set;}
        public string? status {get; set;}
        public int totalProduct {get; set;}
    }
}