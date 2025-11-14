namespace dotnet.Model;
public class Account
{
    public int id { get; set; }
    public string? email { get; set; }
    public string? lastname { get; set; }
    public string? firstname { get; set; }
    public DateTime? bod { get; set; }
    public string? password { get; set; }
    public int role { get; set; } = 3;
    public string? avatarimg { get; set; }
    public DateTime? createdate { get; set; }
    public DateTime? updatedate { get; set; }
    public bool isdeleted { get; set; } = false;
    public string? refreshtoken { get; set; }
    public DateTime? refreshtokenexpires { get; set; }

    public ICollection<Address> addresses { get; set; } = new List<Address>();
    public ICollection<Order> orders { get; set; } = new List<Order>();
    public ICollection<WishList> wishlists { get; set; } = new List<WishList>();
    public ICollection<ShoppingCart> carts { get; set; } = new List<ShoppingCart>();
}
