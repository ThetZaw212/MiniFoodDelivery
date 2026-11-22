using Catalog.Api.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks.Grpc;

namespace Catalog.Api.Grpc;

public class CatalogGrpcService(CatalogDbContext db, ILogger<CatalogGrpcService> logger) : CatalogGrpc.CatalogGrpcBase
{
    public override async Task<GetMenuItemReply> GetMenuItem(GetMenuItemRequest request, ServerCallContext context)
    {
        logger.LogInformation("Sync gRPC GetMenuItem {MenuItemId}", request.MenuItemId);

        if (!Guid.TryParse(request.MenuItemId, out var id))
            return new GetMenuItemReply { Found = false };

        var item = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Restaurant)
            .FirstOrDefaultAsync(x => x.Id == id, context.CancellationToken);

        if (item is null)
            return new GetMenuItemReply { Found = false };

        return new GetMenuItemReply
        {
            Found = true,
            Id = item.Id.ToString(),
            RestaurantId = item.RestaurantId.ToString(),
            RestaurantName = item.Restaurant?.Name ?? "",
            Name = item.Name,
            Price = item.Price.ToString("0.00"),
            Available = item.IsAvailable
        };
    }
}
