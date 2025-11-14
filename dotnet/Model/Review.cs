namespace dotnet.Model;
public class Review
{
    public int id { get; set; }
<<<<<<< HEAD
    public int orderid { get; set; }
    public string? content { get; set; }
    public int rating { get; set; }
=======
    public string? content { get; set; }
    public int rating { get; set; }
    public int orderdetail_id { get; set; }
>>>>>>> 337f3c50ea813517e90e1dd2cf24129c526ddc69
    public string[]? imageurls { get; set; }
    public DateTime? createdate { get; set; }
    public DateTime? updatedate { get; set; }
    public bool isupdated { get; set; } = false;
<<<<<<< HEAD

    public Order order { get; set; } = null!;
=======
public OrderDetail orderdetail { get; set; }
>>>>>>> 337f3c50ea813517e90e1dd2cf24129c526ddc69
}