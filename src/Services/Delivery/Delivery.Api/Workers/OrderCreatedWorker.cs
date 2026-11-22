using System.Text.Json;
using Delivery.Api.Data;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;

namespace Delivery.Api.Workers;

public class OrderCreatedWorker(IServiceProvider services, ILogger<OrderCreatedWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        RedisSubscriber.ListenAsync(Channels.Orders, HandleAsync, logger, stoppingToken);

    private async Task HandleAsync(string json)
    {
        var message = JsonSerializer.Deserialize<EventMessage>(json, Json);
        if (message is null || message.Name != EventNames.OrderCreated)
            return;

        var order = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload, Json);
        if (order is null)
            return;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var exists = await db.Deliveries.AnyAsync(x => x.OrderId == order.OrderId);
        if (exists)
        {
            logger.LogInformation("Delivery already exists for order {OrderId}", order.OrderId);
            return;
        }

        db.Deliveries.Add(new DeliveryJob
        {
            Id = Guid.NewGuid(),
            OrderId = order.OrderId,
            RestaurantName = order.RestaurantName,
            CustomerName = order.CustomerName,
            Address = order.DeliveryAddress,
            Status = "WaitingForCourier",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Async handler created a delivery for order {OrderId}", order.OrderId);
    }
}
