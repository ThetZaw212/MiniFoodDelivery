using Grpc.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks.Grpc;
using Ordering.Api.Contracts;
using Ordering.Api.Data;

namespace Ordering.Api.Features;

public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

public record ListOrdersQuery(string? CustomerName) : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetOrderHandler(
    OrderingDbContext db,
    DeliveryGrpc.DeliveryGrpcClient delivery,
    ILogger<GetOrderHandler> logger) : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (order is null)
            return null;

        var (status, courier) = await ReadDeliveryAsync(delivery, logger, order.Id, cancellationToken);
        return OrderMapper.ToDto(order, status, courier);
    }

    internal static async Task<(string Status, string? Courier)> ReadDeliveryAsync(
        DeliveryGrpc.DeliveryGrpcClient delivery,
        ILogger logger,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var reply = await delivery.GetByOrderIdAsync(
                new GetByOrderIdRequest { OrderId = orderId.ToString() },
                cancellationToken: cancellationToken);

            if (!reply.Found)
                return ("not-created-yet", null);

            return (reply.Status, string.IsNullOrWhiteSpace(reply.CourierName) ? null : reply.CourierName);
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "Sync gRPC call to Delivery failed for order {OrderId}", orderId);
            return ("delivery-service-unavailable", null);
        }
    }
}

public sealed class ListOrdersHandler(OrderingDbContext db, DeliveryGrpc.DeliveryGrpcClient delivery, ILogger<ListOrdersHandler> logger)
    : IRequestHandler<ListOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.CustomerName))
            query = query.Where(x => x.CustomerName == request.CustomerName);

        var orders = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        var results = new List<OrderDto>();
        foreach (var order in orders)
        {
            var (status, courier) = await GetOrderHandler.ReadDeliveryAsync(delivery, logger, order.Id, cancellationToken);
            results.Add(OrderMapper.ToDto(order, status, courier));
        }

        return results;
    }
}
