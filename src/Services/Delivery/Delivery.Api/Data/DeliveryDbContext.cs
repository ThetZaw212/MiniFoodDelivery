using Microsoft.EntityFrameworkCore;

namespace Delivery.Api.Data;

public class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    public DbSet<DeliveryJob> Deliveries => Set<DeliveryJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryJob>(job =>
        {
            job.HasKey(x => x.Id);
            job.HasIndex(x => x.OrderId).IsUnique();
            job.Property(x => x.CustomerName).HasMaxLength(120);
            job.Property(x => x.Address).HasMaxLength(240);
            job.Property(x => x.RestaurantName).HasMaxLength(120);
            job.Property(x => x.Status).HasMaxLength(40);
            job.Property(x => x.CourierName).HasMaxLength(120);
        });
    }
}

public class DeliveryJob
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string RestaurantName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Status { get; set; } = "WaitingForCourier";
    public string? CourierName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
