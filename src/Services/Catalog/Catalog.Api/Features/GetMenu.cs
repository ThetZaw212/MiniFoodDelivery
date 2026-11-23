using System.Text.Json;
using Catalog.Api.Contracts;
using Catalog.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Api.Features;

public record GetMenuQuery(Guid RestaurantId) : IRequest<CachedResult<MenuDto>?>;

public sealed class GetMenuHandler(CatalogDbContext db, IDistributedCache cache, ILogger<GetMenuHandler> logger)
    : IRequestHandler<GetMenuQuery, CachedResult<MenuDto>?>
{
    public static string CacheKey(Guid restaurantId) => $"catalog:menu:{restaurantId}";

    public async Task<CachedResult<MenuDto>?> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKey(request.RestaurantId);
        try
        {
            var cached = await cache.GetStringAsync(key, cancellationToken);
            if (cached is not null)
            {
                var hit = JsonSerializer.Deserialize<MenuDto>(cached);
                if (hit is not null)
                    return new CachedResult<MenuDto>(hit, true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache read failed. Reading menu from SQL.");
        }

        var restaurant = await db.Restaurants
            .AsNoTracking()
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.Id == request.RestaurantId, cancellationToken);

        if (restaurant is null)
            return null;

        var menu = new MenuDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.MenuItems
                .OrderBy(x => x.Name)
                .Select(x => new MenuItemDto(x.Id, x.Name, x.Description, x.Price, x.IsAvailable, x.Stock))
                .ToList());

        try
        {
            await cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(menu),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache write failed.");
        }

        return new CachedResult<MenuDto>(menu, false);
    }
}
