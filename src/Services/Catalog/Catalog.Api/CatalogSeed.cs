using Catalog.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api;

public static class CatalogSeed
{
    public static readonly Guid GoldenBowl = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TeaLeafSalad = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid Mohinga = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid RiverTea = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ShanNoodles = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid MilkTea = Guid.Parse("22222222-2222-2222-2222-222222222202");

    public static async Task SeedAsync(CatalogDbContext db)
    {
        if (await db.Restaurants.AnyAsync())
            return;

        db.Restaurants.AddRange(
            new Restaurant
            {
                Id = GoldenBowl,
                Name = "Golden Bowl",
                Cuisine = "Burmese",
                Address = "Pansodan Street, Yangon",
                IsOpen = true,
                MenuItems =
                [
                    new MenuItem { Id = TeaLeafSalad, Name = "Tea Leaf Salad", Description = "Laphet thoke", Price = 4500m },
                    new MenuItem { Id = Mohinga, Name = "Mohinga", Description = "Fish noodle soup", Price = 3500m }
                ]
            },
            new Restaurant
            {
                Id = RiverTea,
                Name = "River Tea House",
                Cuisine = "Shan",
                Address = "Inle Lake Road",
                IsOpen = true,
                MenuItems =
                [
                    new MenuItem { Id = ShanNoodles, Name = "Shan Noodles", Description = "Rice noodles with chicken", Price = 4000m },
                    new MenuItem { Id = MilkTea, Name = "Milk Tea", Description = "Hot milk tea", Price = 1500m }
                ]
            });

        await db.SaveChangesAsync();
    }
}
