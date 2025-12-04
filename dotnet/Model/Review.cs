namespace dotnet.Model;
public class Review
{
    public int id { get; set; }
<<<<<<< HEAD
    public string? content { get; set; }
    public int rating { get; set; }
    public int orderdetail_id { get; set; }
=======
    public int orderid { get; set; }
    public string? content { get; set; }
    public int rating { get; set; }
>>>>>>> user
    public string[]? imageurls { get; set; }
    public DateTime? createdate { get; set; }
    public DateTime? updatedate { get; set; }
    public bool isupdated { get; set; } = false;
<<<<<<< HEAD
    public OrderDetail orderdetail { get; set; } = null!; // set by EF
}
=======

    public Order order { get; set; } = null!;
}
>>>>>>> user
