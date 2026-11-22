using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Restaurant>(restaurant =>
        {
            restaurant.HasKey(x => x.Id);
            restaurant.Property(x => x.Name).HasMaxLength(120).IsRequired();
            restaurant.Property(x => x.Cuisine).HasMaxLength(80).IsRequired();
            restaurant.Property(x => x.Address).HasMaxLength(200).IsRequired();
            restaurant.HasMany(x => x.MenuItems).WithOne(x => x.Restaurant).HasForeignKey(x => x.RestaurantId);
        });

        modelBuilder.Entity<MenuItem>(item =>
        {
            item.HasKey(x => x.Id);
            item.Property(x => x.Name).HasMaxLength(120).IsRequired();
            item.Property(x => x.Description).HasMaxLength(400);
            item.Property(x => x.Price).HasPrecision(18, 2);
        });
    }
}

public class Restaurant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Cuisine { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsOpen { get; set; } = true;
    public List<MenuItem> MenuItems { get; set; } = [];
}

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public Restaurant? Restaurant { get; set; }
}
