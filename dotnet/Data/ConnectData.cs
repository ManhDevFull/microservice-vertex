using be_dotnet_ecommerce1.Model;
using dotnet.Dtos;
using dotnet.Dtos.admin;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace be_dotnet_ecommerce1.Data
{
  public class ConnectData : DbContext
  {
    public ConnectData(DbContextOptions<ConnectData> options) : base(options) { }

    public const string ProductAdminSql = @"
SELECT
  p.id AS product_id,
  p.nameproduct AS name,
  COALESCE(b.name, '') AS brand,
  p.description,
  p.category AS category_id,
  c.namecategory AS category_name,
  p.imageurls,
  p.createdate,
  p.updatedate,
  COALESCE(
    jsonb_agg(
      jsonb_build_object(
        'variant_id',   v.id,
        'product_id',   v.product_id,
        'valuevariant', v.valuevariant,
        'stock',        v.stock,
        'inputprice',   v.inputprice,
        'price',        v.price,
        'createdate',   v.createdate,
        'updatedate',   v.updatedate,
        'isdeleted',    v.isdeleted,
        'sold',         COALESCE(s.units_sold, 0)::int
      )
      ORDER BY v.id
    ) FILTER (WHERE v.id IS NOT NULL),
    '[]'::jsonb
  ) AS variants,
  COUNT(v.id)::int AS variant_count,
  MIN(v.price) AS min_price,
  MAX(v.price) AS max_price
FROM product p
LEFT JOIN brand b ON b.id = p.brand_id
LEFT JOIN category c ON c.id = p.category
LEFT JOIN variant v ON v.product_id = p.id AND NOT v.isdeleted
LEFT JOIN LATERAL (
  SELECT SUM(o.quantity) AS units_sold
  FROM orders o
  WHERE o.variant_id = v.id
    AND o.statusorder = 'DELIVERED'
) s ON TRUE
WHERE NOT p.isdeleted
GROUP BY
  p.id,
  p.nameproduct,
  b.name,
  p.description,
  p.category,
  c.namecategory,
  p.imageurls,
  p.createdate,
  p.updatedate";

    // Entities
    public DbSet<Account> accounts { get; set; } = null!;
    public DbSet<Address> address { get; set; } = null!;
    public DbSet<Brand> brands { get; set; } = null!;
    public DbSet<Category> categories { get; set; } = null!;
    public DbSet<CategoryBrandStats> category_brand_stats { get; set; } = null!;
    public DbSet<Discount> discounts { get; set; } = null!;
    public DbSet<DiscountProduct> discountProducts { get; set; } = null!;
    public DbSet<Order> orders { get; set; } = null!;
    public DbSet<Product> products { get; set; } = null!;
    public DbSet<Review> reviews { get; set; } = null!;
    public DbSet<ShoppingCart> shoppingCarts { get; set; } = null!;
    public DbSet<Variant> variants { get; set; } = null!;
    public DbSet<WishList> wishLists { get; set; } = null!;
    public DbSet<EmailVerification> emailVerifications { get; set; } = null!;

    // DTO / Views
    public DbSet<CategoryAdminDTO> categoryAdmins { get; set; } = null!;
    public DbSet<UserAdminDTO> userAdmins { get; set; } = null!;
    public DbSet<UserDTO> userDtos { get; set; } = null!;
    public DbSet<ProductAdminDTO> productAdmins { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      // -------- account --------
      modelBuilder.Entity<Account>(e =>
      {
        e.ToTable("account");
        e.HasKey(x => x.id);
        e.Property(x => x.email).HasColumnName("email");
        e.Property(x => x.lastname).HasColumnName("lastname");
        e.Property(x => x.firstname).HasColumnName("firstname");
        e.Property(x => x.bod).HasColumnName("bod");
        e.Property(x => x.password).HasColumnName("password");
        e.Property(x => x.role).HasColumnName("role");
        e.Property(x => x.avatarimg).HasColumnName("avatarimg");
        e.Property(x => x.createdate).HasColumnName("createdate");
        e.Property(x => x.updatedate).HasColumnName("updatedate");
        e.Property(x => x.isdeleted).HasColumnName("isdeleted");
        e.Property(x => x.refreshtoken).HasColumnName("refreshtoken");
        e.Property(x => x.refreshtokenexpires).HasColumnName("refreshtokenexpires");
      });

      modelBuilder.Entity<UserDTO>(e =>
      {
        e.HasNoKey();
        e.ToSqlQuery(@"
          SELECT
            0 AS id,
            '' AS name,
            '' AS email,
            0 AS role,
            '' AS avatarimg,
            '' AS tel,
            0 AS orders
          WHERE 1 = 0");
        e.Property(x => x.id).HasColumnName("id");
        e.Property(x => x.name).HasColumnName("name");
        e.Property(x => x.email).HasColumnName("email");
        e.Property(x => x.role).HasColumnName("role");
        e.Property(x => x.avatarImg).HasColumnName("avatarimg");
        e.Property(x => x.tel).HasColumnName("tel");
        e.Property(x => x.orders).HasColumnName("orders");
      });
      // -------- email_verification --------
      modelBuilder.Entity<EmailVerification>(e =>
      {
        e.ToTable("email_verification");
        e.HasKey(x => x.id);
        e.HasIndex(x => x.email).IsUnique();
        e.Property(x => x.email).HasColumnName("email");
        e.Property(x => x.codehash).HasColumnName("codehash");
        e.Property(x => x.passwordhash).HasColumnName("passwordhash");
        e.Property(x => x.firstname).HasColumnName("firstname");
        e.Property(x => x.lastname).HasColumnName("lastname");
        e.Property(x => x.expiresat).HasColumnName("expiresat");
        e.Property(x => x.createdat).HasColumnName("createdat");
        e.Property(x => x.updatedat).HasColumnName("updatedat");
        e.Property(x => x.attemptcount).HasColumnName("attemptcount");
        e.Property(x => x.lastsentat).HasColumnName("lastsentat");
      });

      // -------- address --------
      modelBuilder.Entity<Address>(e =>
      {
        e.ToTable("address");
        e.HasKey(x => x.id);
        e.Property(x => x.accountid).HasColumnName("account_id");
        e.Property(x => x.title).HasColumnName("title");
        e.Property(x => x.namerecipient).HasColumnName("namerecipient");
        e.Property(x => x.tel).HasColumnName("tel");
        e.Property(x => x.codeward).HasColumnName("codeward");
        e.Property(x => x.description).HasColumnName("description");
        e.Property(x => x.detail).HasColumnName("detail");
        e.Property(x => x.createdate).HasColumnName("createdate");
        e.Property(x => x.updatedate).HasColumnName("updatedate");

        e.HasOne(x => x.account)
         .WithMany(a => a.addresses)
         .HasForeignKey(x => x.accountid)
         .OnDelete(DeleteBehavior.Cascade);
      });

      // -------- brand --------
      modelBuilder.Entity<Brand>(e =>
      {
        e.ToTable("brand");
        e.HasKey(x => x.id);
        e.Property(x => x.name).HasColumnName("name");
      });

      // -------- category --------
      modelBuilder.Entity<Category>(e =>
      {
        e.ToTable("category");
        e.HasKey(x => x.id);
        e.Property(x => x.namecategory).HasColumnName("namecategory");
        e.Property(x => x.idparent).HasColumnName("parent_id");

        e.HasOne(x => x.Parent)
         .WithMany(p => p.Children)
         .HasForeignKey(x => x.idparent)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.Products)
         .WithOne(p => p.category)
         .HasForeignKey(p => p.categoryId)
         .OnDelete(DeleteBehavior.Restrict);
      });
      // -------- CategoryBrandStats --------
      modelBuilder.Entity<CategoryBrandStats>(e =>
      {
        e.ToTable("category_brand_stats");
        e.HasKey(x => new { x.category_id, x.brand_id });
        e.Property(x => x.category_id).HasColumnName("category_id");
        e.Property(x => x.brand_id).HasColumnName("brand_id");
        e.Property(x => x.product_count).HasColumnName("product_count");
        e.Property(x => x.variant_count).HasColumnName("variant_count");
        e.Property(x => x.units_sold).HasColumnName("units_sold");
        e.Property(x => x.revenue).HasColumnName("revenue");
        e.Property(x => x.updated_at).HasColumnName("updated_at");
      });
      // -------- discount --------
      modelBuilder.Entity<Discount>(e =>
      {
        e.ToTable("discount");
        e.HasKey(x => x.id);
        e.Property(x => x.typediscount).HasColumnName("typediscount");
        e.Property(x => x.discount).HasColumnName("discount");
        e.Property(x => x.starttime).HasColumnName("starttime");
        e.Property(x => x.endtime).HasColumnName("endtime");
        e.Property(x => x.createtime).HasColumnName("createtime");
      });

      // -------- discount_product --------
      modelBuilder.Entity<DiscountProduct>(e =>
      {
        e.ToTable("discount_product");
        e.HasKey(x => x.id);
        e.Property(x => x.discountid).HasColumnName("discount_id");
        e.Property(x => x.variantid).HasColumnName("variant_id");

        e.HasOne(x => x.variant)
         .WithMany(v => v.discountProduct)
         .HasForeignKey(x => x.variantid)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.discount)
         .WithMany(d => d.discountProducts)
         .HasForeignKey(x => x.discountid)
         .OnDelete(DeleteBehavior.Restrict);
      });

      // -------- orders --------
      modelBuilder.Entity<Order>(e =>
      {
        e.ToTable("orders");
        e.HasKey(x => x.id);
        e.Property(x => x.accountid).HasColumnName("account_id");
        e.Property(x => x.variantid).HasColumnName("variant_id");
        e.Property(x => x.addressid).HasColumnName("address_id");
        e.Property(x => x.quantity).HasColumnName("quantity");
        e.Property(x => x.orderdate).HasColumnName("orderdate");
        e.Property(x => x.statusorder).HasColumnName("statusorder");
        e.Property(x => x.receivedate).HasColumnName("receivedate");
        e.Property(x => x.typepay).HasColumnName("typepay");
        e.Property(x => x.statuspay).HasColumnName("statuspay");

        e.HasOne(x => x.account).WithMany(a => a.orders)
         .HasForeignKey(x => x.accountid).OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.address).WithMany(a => a.orders)
         .HasForeignKey(x => x.addressid).OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.variant).WithMany(v => v.orders)
         .HasForeignKey(x => x.variantid).OnDelete(DeleteBehavior.Restrict);
      });

      // -------- product --------
      modelBuilder.Entity<Product>(e =>
 {
   e.ToTable("product");
   e.HasKey(x => x.id);

   e.Property(x => x.nameproduct).HasColumnName("nameproduct");
   e.Property(x => x.brand_id).HasColumnName("brand_id");       // FK cột số
   e.Property(x => x.description).HasColumnName("description");
   e.Property(x => x.categoryId).HasColumnName("category");
   e.Property(x => x.imageurls).HasColumnName("imageurls").HasColumnType("text[]");
   e.Property(x => x.createdate).HasColumnName("createdate");
   e.Property(x => x.updatedate).HasColumnName("updatedate");
   e.Property(x => x.isdeleted).HasColumnName("isdeleted");

   // Category -> Products
   e.HasOne(x => x.category)
    .WithMany(c => c.Products)
    .HasForeignKey(x => x.categoryId)
    .OnDelete(DeleteBehavior.Restrict);

   // Brand -> Products  (SỬA Ở ĐÂY)
   e.HasOne(x => x.brand)                 // navigation property kiểu Brand
    .WithMany(b => b.products)            // collection bên Brand
    .HasForeignKey(x => x.brand_id)       // cột FK
    .OnDelete(DeleteBehavior.Restrict);
 });

      // -------- review --------
      modelBuilder.Entity<Review>(e =>
      {
        e.ToTable("review");
        e.HasKey(x => x.id);
        e.Property(x => x.orderid).HasColumnName("order_id");
        e.Property(x => x.content).HasColumnName("content");
        e.Property(x => x.rating).HasColumnName("rating");
        e.Property(x => x.imageurls).HasColumnName("imageurls").HasColumnType("text[]");
        e.Property(x => x.createdate).HasColumnName("createdate");
        e.Property(x => x.updatedate).HasColumnName("updatedate");
        e.Property(x => x.isupdated).HasColumnName("isupdated");

        e.HasOne(x => x.order)
         .WithOne(o => o.review)
         .HasForeignKey<Review>(x => x.orderid)
         .OnDelete(DeleteBehavior.Cascade);
      });

      // -------- shoppingcart --------
      modelBuilder.Entity<ShoppingCart>(e =>
      {
        e.ToTable("shoppingcart");
        e.HasKey(x => x.id);
        e.Property(x => x.accountid).HasColumnName("account_id");
        e.Property(x => x.variantid).HasColumnName("variant_id");
        e.Property(x => x.quantity).HasColumnName("quantity");

        e.HasOne(x => x.account).WithMany(a => a.carts)
         .HasForeignKey(x => x.accountid).OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.variant).WithMany(v => v.carts)
         .HasForeignKey(x => x.variantid).OnDelete(DeleteBehavior.Restrict);
      });

      // -------- variant --------
      modelBuilder.Entity<Variant>(e =>
      {
        e.ToTable("variant");
        e.HasKey(x => x.id);
        e.Property(x => x.productid).HasColumnName("product_id");
        e.Property(x => x.valuevariant).HasColumnName("valuevariant").HasColumnType("jsonb");
        e.Property(x => x.stock).HasColumnName("stock");
        e.Property(x => x.inputprice).HasColumnName("inputprice");
        e.Property(x => x.price).HasColumnName("price");
        e.Property(x => x.createdate).HasColumnName("createdate");
        e.Property(x => x.updatedate).HasColumnName("updatedate");
        e.Property(x => x.isdeleted).HasColumnName("isdeleted");

        e.HasOne(x => x.product)
         .WithMany(p => p.variants)
         .HasForeignKey(x => x.productid)
         .OnDelete(DeleteBehavior.Restrict);
      });

      // -------- wishlist --------
      modelBuilder.Entity<WishList>(e =>
      {
        e.ToTable("wishlist");
        e.HasKey(x => x.id);
        e.Property(x => x.accountid).HasColumnName("account_id");
        e.Property(x => x.productid).HasColumnName("product_id");

        e.HasOne(x => x.product).WithMany(p => p.wishLists)
         .HasForeignKey(x => x.productid).OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.account).WithMany(a => a.wishlists)
         .HasForeignKey(x => x.accountid).OnDelete(DeleteBehavior.Restrict);
      });

      // -------- Admin DTO / Views --------
      modelBuilder.Entity<CategoryAdminDTO>().HasNoKey().ToView(null);
      modelBuilder.Entity<UserAdminDTO>().HasNoKey().ToView(null);
      modelBuilder.Entity<ProductAdminDTO>(e =>
      {
        e.HasNoKey();
        e.ToSqlQuery(ProductAdminSql);
        e.Property(x => x.product_id).HasColumnName("product_id");
        e.Property(x => x.name).HasColumnName("name");
        e.Property(x => x.brand).HasColumnName("brand");
        e.Property(x => x.description).HasColumnName("description");
        e.Property(x => x.category_id).HasColumnName("category_id");
        e.Property(x => x.category_name).HasColumnName("category_name");
        e.Property(x => x.imageurls).HasColumnName("imageurls");
        e.Property(x => x.createdate).HasColumnName("createdate");
        e.Property(x => x.updatedate).HasColumnName("updatedate");
        e.Property(x => x.variants).HasColumnName("variants");
        e.Property(x => x.variant_count).HasColumnName("variant_count");
        e.Property(x => x.min_price).HasColumnName("min_price");
        e.Property(x => x.max_price).HasColumnName("max_price");
      });

      base.OnModelCreating(modelBuilder);
    }

  }
}
