using Catalog.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Api.Controllers;

public record StockLineRequest(Guid MenuItemId, int Quantity);

public record StockRequest(List<StockLineRequest> Lines);

public record StockLineResult(Guid MenuItemId, string Name, int Requested, int Available, bool Ok);

public record StockResult(bool Ok, IReadOnlyList<StockLineResult> Lines);

[ApiController]
[Route("api/stock")]
public class StockController(CatalogDbContext db, IDistributedCache cache) : ControllerBase
{
    [HttpPost("check")]
    public async Task<StockResult> Check(StockRequest request, CancellationToken cancellationToken)
    {
        var lines = await ReadAsync(request.Lines ?? [], cancellationToken);
        return new StockResult(lines.All(x => x.Ok), lines);
    }

    [HttpPost("reserve")]
    public async Task<ActionResult<StockResult>> Reserve(StockRequest request, CancellationToken cancellationToken)
    {
        var incoming = request.Lines ?? [];
        if (incoming.Count == 0 || incoming.Any(x => x.Quantity <= 0))
            return BadRequest(new StockResult(false, []));

        var strategy = db.Database.CreateExecutionStrategy();
        var restaurantIds = new HashSet<Guid>();
        var reserved = false;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            restaurantIds.Clear();
            foreach (var line in incoming)
            {
                var updated = await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE MenuItems SET Stock = Stock - {line.Quantity} WHERE Id = {line.MenuItemId} AND IsAvailable = 1 AND Stock >= {line.Quantity}",
                    cancellationToken);
                if (updated == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    reserved = false;
                    return;
                }

                var restaurantId = await db.MenuItems
                    .Where(x => x.Id == line.MenuItemId)
                    .Select(x => x.RestaurantId)
                    .FirstAsync(cancellationToken);
                restaurantIds.Add(restaurantId);
            }

            await transaction.CommitAsync(cancellationToken);
            reserved = true;
        });

        if (!reserved)
        {
            var snapshot = await ReadAsync(incoming, cancellationToken);
            return Conflict(new StockResult(false, snapshot));
        }
        await DropMenuCacheAsync(restaurantIds, cancellationToken);
        var snapshot = await ReadAsync(incoming, cancellationToken);
        return Ok(new StockResult(true, snapshot));
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(StockRequest request, CancellationToken cancellationToken)
    {
        var restaurantIds = new HashSet<Guid>();
        foreach (var line in request.Lines ?? [])
        {
            if (line.Quantity <= 0)
                continue;
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE MenuItems SET Stock = Stock + {line.Quantity} WHERE Id = {line.MenuItemId}",
                cancellationToken);
            var restaurantId = await db.MenuItems
                .Where(x => x.Id == line.MenuItemId)
                .Select(x => (Guid?)x.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId is Guid id)
                restaurantIds.Add(id);
        }

        await DropMenuCacheAsync(restaurantIds, cancellationToken);
        return NoContent();
    }

    private async Task<List<StockLineResult>> ReadAsync(IReadOnlyList<StockLineRequest> lines, CancellationToken cancellationToken)
    {
        var ids = lines.Select(x => x.MenuItemId).ToList();
        var items = await db.MenuItems.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        return lines.Select(line =>
        {
            var item = items.FirstOrDefault(x => x.Id == line.MenuItemId);
            var available = item is { IsAvailable: true } ? item.Stock : 0;
            return new StockLineResult(line.MenuItemId, item?.Name ?? "Unknown dish", line.Quantity, available, item is not null && available >= line.Quantity);
        }).ToList();
    }

    private async Task DropMenuCacheAsync(IEnumerable<Guid> restaurantIds, CancellationToken cancellationToken)
    {
        foreach (var id in restaurantIds)
        {
            try
            {
                await cache.RemoveAsync($"catalog:menu:{id}", cancellationToken);
            }
            catch
            {
                // The next menu read falls back to SQL when Redis is down.
            }
        }
    }
}
