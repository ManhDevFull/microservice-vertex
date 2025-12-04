namespace dotnet.Dtos
{
    public class DiscountDTO
    {
        public int id { get; set; }
        public int? typediscount { get; set; }
        public int? discount { get; set; }
        public DateTime starttime { get; set; }
        public DateTime endtime { get; set; }
        //public DateTime? createtime { get; set; }
    }
}