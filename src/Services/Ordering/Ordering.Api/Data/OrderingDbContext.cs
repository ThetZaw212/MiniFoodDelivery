using Microsoft.EntityFrameworkCore;

namespace Ordering.Api.Data;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<CustomerOrder> Orders => Set<CustomerOrder>();
    public DbSet<OrderLine> Lines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerOrder>(order =>
        {
            order.HasKey(x => x.Id);
            order.Property(x => x.RestaurantName).HasMaxLength(120);
            order.Property(x => x.CustomerName).HasMaxLength(120);
            order.Property(x => x.DeliveryAddress).HasMaxLength(240);
            order.Property(x => x.Status).HasMaxLength(40);
            order.Property(x => x.Total).HasPrecision(18, 2);
            order.HasMany(x => x.Lines).WithOne(x => x.Order).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderLine>(line =>
        {
            line.HasKey(x => x.Id);
            line.Property(x => x.Name).HasMaxLength(120);
            line.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });
    }
}

public class CustomerOrder
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string Status { get; set; } = "Placed";
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public CustomerOrder? Order { get; set; }
}
