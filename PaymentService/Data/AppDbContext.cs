using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentTransaction>(e =>
            {
                e.ToTable("payment_transaction");
                e.HasKey(x => x.Id);
                
                // Map all properties to snake_case column names
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.OrderId).HasColumnName("order_id").IsRequired().HasMaxLength(100);
                e.Property(x => x.AccountId).HasColumnName("account_id");
                e.Property(x => x.PartnerCode).HasColumnName("partner_code").IsRequired().HasMaxLength(50);
                e.Property(x => x.RequestId).HasColumnName("request_id").IsRequired().HasMaxLength(100);
                e.Property(x => x.Amount).HasColumnName("amount");
                e.Property(x => x.OrderInfo).HasColumnName("order_info").IsRequired().HasMaxLength(500);
                e.Property(x => x.PaymentUrl).HasColumnName("payment_url");
                e.Property(x => x.QrCode).HasColumnName("qr_code");
                e.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
                e.Property(x => x.MoMoTransactionId).HasColumnName("momo_transaction_id");
                e.Property(x => x.ResponseCode).HasColumnName("response_code");
                e.Property(x => x.Message).HasColumnName("message");
                e.Property(x => x.Signature).HasColumnName("signature");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                e.Property(x => x.PaidAt).HasColumnName("paid_at");
                
                e.HasIndex(x => x.OrderId).IsUnique();
                e.HasIndex(x => x.RequestId).IsUnique();
                e.HasIndex(x => x.AccountId);
                e.HasIndex(x => x.Status);
            });
        }
    }
}

