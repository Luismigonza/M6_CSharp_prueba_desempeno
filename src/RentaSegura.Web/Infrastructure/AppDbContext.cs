using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure.Interceptors;

namespace RentaSegura.Web.Infrastructure;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<Notification> Notifications   => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(150).IsRequired();
            e.Property(p => p.City).HasMaxLength(100).IsRequired();
            e.Property(p => p.Address).HasMaxLength(250).IsRequired();
            e.Property(p => p.Description).HasMaxLength(2000).IsRequired();
            e.Property(p => p.PricePerNight).HasPrecision(12, 2);
            e.Property(p => p.LastModifiedBy).HasMaxLength(450);
            e.HasIndex(p => p.City);
            e.HasIndex(p => p.OwnerId);
        });

        builder.Entity<Reservation>(e =>
        {
            e.ToTable("Reservations");
            e.Property(r => r.PropertyId).HasColumnName("PropertyId");
            e.Property(r => r.CheckInDate).HasColumnName("CheckInDate");
            e.Property(r => r.CheckOutDate).HasColumnName("CheckOutDate");
            e.Property(r => r.Status).HasColumnName("Status").HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.PricePaid).HasPrecision(12, 2);
            e.Property(r => r.LastModifiedBy).HasMaxLength(450);
            e.HasIndex(r => r.GuestUserId);
            e.HasOne(r => r.Property).WithMany(p => p.Reservations).HasForeignKey(r => r.PropertyId);
        });

        builder.Entity<Favorite>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.PropertyId }).IsUnique();
            e.HasOne(f => f.Property).WithMany().HasForeignKey(f => f.PropertyId);
        });

        builder.Entity<KycVerification>(e =>
        {
            e.Property(k => k.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(k => k.UserId);
        });

        builder.Entity<Notification>(e =>
        {
            e.Property(n => n.Title).HasMaxLength(150).IsRequired();
            e.Property(n => n.Body).HasMaxLength(1000).IsRequired();
            e.HasIndex(n => n.RecipientUserId);
            // Índice compuesto para consultas de "no leídas por usuario".
            e.HasIndex(n => new { n.RecipientUserId, n.IsRead });
        });
    }
}