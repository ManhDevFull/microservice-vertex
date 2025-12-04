using Microsoft.EntityFrameworkCore;
using ShippingService.Models;

namespace ShippingService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();
        public DbSet<ShippingCarrier> ShippingCarriers => Set<ShippingCarrier>();
        public DbSet<ShippingOption> ShippingOptions => Set<ShippingOption>();
        public DbSet<CheckoutSelection> CheckoutSelections => Set<CheckoutSelection>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>(e =>
            {
                e.ToTable("payment_provider");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Code).IsUnique();
                e.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<ShippingCarrier>(e =>
            {
                e.ToTable("shipping_carrier");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Code).IsUnique();
                e.Property(x => x.Name).IsRequired();
            });

            modelBuilder.Entity<ShippingOption>(e =>
            {
                e.ToTable("shipping_option");
                e.HasKey(x => x.Id);

                e.HasOne(x => x.Carrier)
                    .WithMany(x => x.Options)
                    .HasForeignKey(x => x.CarrierId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.CarrierId, x.Code }).IsUnique();
            });

            modelBuilder.Entity<CheckoutSelection>(e =>
            {
                e.ToTable("checkout_selection");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.AccountId);
            });
        }
    }
}
