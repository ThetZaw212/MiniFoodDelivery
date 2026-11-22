using Catalog.Api.Contracts;
using Catalog.Api.Data;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Api.Features;

public record CreateRestaurantCommand(string Name, string Cuisine, string Address) : IRequest<RestaurantDto>;

public sealed class CreateRestaurantHandler(CatalogDbContext db, IDistributedCache cache)
    : IRequestHandler<CreateRestaurantCommand, RestaurantDto>
{
    public async Task<RestaurantDto> Handle(CreateRestaurantCommand request, CancellationToken cancellationToken)
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Cuisine = request.Cuisine.Trim(),
            Address = request.Address.Trim(),
            IsOpen = true
        };

        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(GetRestaurantsHandler.CacheKey, cancellationToken);
        return new RestaurantDto(restaurant.Id, restaurant.Name, restaurant.Cuisine, restaurant.Address, restaurant.IsOpen);
    }
}
