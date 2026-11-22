using Catalog.Api.Contracts;
using Catalog.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MiniFood.BuildingBlocks;

namespace Catalog.Api.Features;

public record AddMenuItemCommand(Guid RestaurantId, string Name, string Description, decimal Price) : IRequest<MenuItemDto>;

public sealed class AddMenuItemHandler(CatalogDbContext db, IDistributedCache cache)
    : IRequestHandler<AddMenuItemCommand, MenuItemDto>
{
    public async Task<MenuItemDto> Handle(AddMenuItemCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Restaurants.AnyAsync(x => x.Id == request.RestaurantId, cancellationToken);
        if (!exists)
            throw new AppException("Restaurant was not found.", StatusCodes.Status404NotFound);

        if (request.Price <= 0)
            throw new AppException("Price must be greater than zero.");

        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            IsAvailable = true
        };

        db.MenuItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(GetMenuHandler.CacheKey(request.RestaurantId), cancellationToken);
        return new MenuItemDto(item.Id, item.Name, item.Description, item.Price, item.IsAvailable);
    }
}
