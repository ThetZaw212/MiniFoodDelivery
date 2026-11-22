using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;
using MiniFood.BuildingBlocks.Grpc;
using Ordering.Api.Contracts;
using Ordering.Api.Data;

namespace Ordering.Api.Features;

public record UpdateOrderStatusCommand(Guid OrderId, string Status) : IRequest<OrderDto>;

public sealed class UpdateOrderStatusHandler(
    OrderingDbContext db,
    DeliveryGrpc.DeliveryGrpcClient delivery,
    ILogger<UpdateOrderStatusHandler> logger) : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private static readonly HashSet<string> Allowed =
    [
        "Placed", "Accepted", "Preparing", "OutForDelivery", "Delivered", "Cancelled"
    ];

    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Allowed.Contains(request.Status))
            throw new AppException($"Status must be one of: {string.Join(", ", Allowed)}.");

        var order = await db.Orders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);
        if (order is null)
            throw new AppException("Order was not found.", StatusCodes.Status404NotFound);

        order.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);

        var (status, courier) = await GetOrderHandler.ReadDeliveryAsync(delivery, logger, order.Id, cancellationToken);
        return OrderMapper.ToDto(order, status, courier);
    }
}
