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
    IReadOnlyList<OrderLineRequest> Lines,
    string? PaymentMethod,
    string? PaymentLabel) : IRequest<OrderDto>;

public sealed class CreateOrderHandler(
    OrderingDbContext db,
    CatalogGrpc.CatalogGrpcClient catalog,
    IHttpClientFactory httpClientFactory,
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

        var paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod.Trim();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Cash", "Bank", "Visa", "Mastercard", "MPU", "PayPal" };
        if (!allowed.Contains(paymentMethod))
            throw new AppException("Choose cash, bank, Visa, Mastercard, MPU, or PayPal.");
        paymentMethod = allowed.First(x => x.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));
        var paymentLabel = string.IsNullOrWhiteSpace(request.PaymentLabel) ? paymentMethod : request.PaymentLabel.Trim();

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
            PaymentMethod = paymentMethod,
            PaymentLabel = paymentLabel,
            Total = total,
            CreatedAtUtc = DateTime.UtcNow,
            Lines = lines
        };

        await ReserveStockAsync(request.Lines, cancellationToken);

        try
        {
            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await ReleaseStockAsync(request.Lines, cancellationToken);
            throw;
        }

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

    private async Task ReserveStockAsync(IReadOnlyList<OrderLineRequest> lines, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("catalog");
        var response = await client.PostAsJsonAsync("/api/stock/reserve", new { lines }, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("Stock reserve failed {Status}: {Body}", (int)response.StatusCode, body);
        throw new AppException("A dish just sold out. Review the basket and try again.", StatusCodes.Status409Conflict);
    }

    private async Task ReleaseStockAsync(IReadOnlyList<OrderLineRequest> lines, CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("catalog");
            await client.PostAsJsonAsync("/api/stock/release", new { lines }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stock was reserved but could not be released.");
        }
    }
}
