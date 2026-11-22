using System.Text.Json;
using Catalog.Api.Contracts;
using Catalog.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Api.Features;

public record GetRestaurantsQuery : IRequest<CachedResult<IReadOnlyList<RestaurantDto>>>;

public sealed class GetRestaurantsHandler(CatalogDbContext db, IDistributedCache cache, ILogger<GetRestaurantsHandler> logger)
    : IRequestHandler<GetRestaurantsQuery, CachedResult<IReadOnlyList<RestaurantDto>>>
{
    public const string CacheKey = "catalog:restaurants";

    public async Task<CachedResult<IReadOnlyList<RestaurantDto>>> Handle(GetRestaurantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await cache.GetStringAsync(CacheKey, cancellationToken);
            if (cached is not null)
            {
                var hit = JsonSerializer.Deserialize<List<RestaurantDto>>(cached) ?? [];
                return new CachedResult<IReadOnlyList<RestaurantDto>>(hit, true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache read failed. Reading restaurants from SQL.");
        }

        var restaurants = await db.Restaurants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RestaurantDto(x.Id, x.Name, x.Cuisine, x.Address, x.IsOpen))
            .ToListAsync(cancellationToken);

        try
        {
            await cache.SetStringAsync(
                CacheKey,
                JsonSerializer.Serialize(restaurants),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache write failed.");
        }

        return new CachedResult<IReadOnlyList<RestaurantDto>>(restaurants, false);
    }
}
