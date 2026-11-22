using System.Globalization;
using Grpc.Core;
using MediatR;
using MiniFood.BuildingBlocks;
using MiniFood.BuildingBlocks.Grpc;
using Ordering.Api.Contracts;
using Ordering.Api.Data;

namespace Ordering.Api.Features;

public record CreateOrderCommand(
    Guid RestaurantId,
    string CustomerName,
    string DeliveryAddress,
    IReadOnlyList<OrderLineRequest> Lines) : IRequest<OrderDto>;

public sealed class CreateOrderHandler(
    OrderingDbContext db,
    CatalogGrpc.CatalogGrpcClient catalog,
    IEventBus bus,
    ILogger<CreateOrderHandler> logger) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new AppException("Customer name is required.");
        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
            throw new AppException("Delivery address is required.");
        if (request.Lines.Count == 0)
            throw new AppException("Add at least one menu item.");

        var lines = new List<OrderLine>();
        string restaurantName = "";
        decimal total = 0;

        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
                throw new AppException("Quantity must be greater than zero.");

            GetMenuItemReply reply;
            try
            {
                reply = await catalog.GetMenuItemAsync(
                    new GetMenuItemRequest { MenuItemId = line.MenuItemId.ToString() },
                    cancellationToken: cancellationToken);
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "Sync gRPC call to Catalog failed");
                throw new AppException("Catalog service is unavailable. The synchronous gRPC call failed.", StatusCodes.Status503ServiceUnavailable);
            }

            if (!reply.Found)
                throw new AppException($"Menu item {line.MenuItemId} was not found.");
            if (!reply.Available)
                throw new AppException($"{reply.Name} is not available.");
            if (!Guid.TryParse(reply.RestaurantId, out var itemRestaurant) || itemRestaurant != request.RestaurantId)
                throw new AppException($"{reply.Name} does not belong to this restaurant.");

            restaurantName = reply.RestaurantName;
            var price = decimal.Parse(reply.Price, CultureInfo.InvariantCulture);
            total += price * line.Quantity;
            lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                MenuItemId = line.MenuItemId,
                Name = reply.Name,
                UnitPrice = price,
                Quantity = line.Quantity
            });
        }

        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            RestaurantName = restaurantName,
            CustomerName = request.CustomerName.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            Status = "Placed",
            Total = total,
            CreatedAtUtc = DateTime.UtcNow,
            Lines = lines
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await bus.PublishAsync(Channels.Orders, EventNames.OrderCreated, new OrderCreatedEvent(
                order.Id,
                order.RestaurantId,
                order.RestaurantName,
                order.CustomerName,
                order.DeliveryAddress,
                order.Total,
                order.CreatedAtUtc), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Order {OrderId} is saved, but Redis did not accept the async event.", order.Id);
        }

        return OrderMapper.ToDto(order, "pending", null);
    }
}
