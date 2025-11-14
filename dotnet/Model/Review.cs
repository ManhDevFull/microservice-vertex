namespace dotnet.Model;
public class Review
{
    public int id { get; set; }
    public string? content { get; set; }
    public int rating { get; set; }
    public int orderdetail_id { get; set; }
    public string[]? imageurls { get; set; }
    public DateTime? createdate { get; set; }
    public DateTime? updatedate { get; set; }
    public bool isupdated { get; set; } = false;
public OrderDetail orderdetail { get; set; }
}