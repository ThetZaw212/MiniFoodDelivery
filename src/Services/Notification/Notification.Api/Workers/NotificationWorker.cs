using System.Net.Http.Json;
using System.Text.Json;
using MiniFood.BuildingBlocks;
using Notification.Api.Data;

namespace Notification.Api.Workers;

public class NotificationWorker(IServiceProvider services, IHttpClientFactory httpClientFactory, ILogger<NotificationWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var orders = RedisSubscriber.ListenAsync(Channels.Orders, json => HandleAsync(json, EventNames.OrderCreated), logger, stoppingToken);
        var deliveries = RedisSubscriber.ListenAsync(Channels.Deliveries, json => HandleAsync(json, EventNames.DeliveryUpdated), logger, stoppingToken);
        await Task.WhenAll(orders, deliveries);
    }

    private async Task HandleAsync(string json, string expectedName)
    {
        var message = JsonSerializer.Deserialize<EventMessage>(json, Json);
        if (message is null || message.Name != expectedName)
            return;

        var summary = BuildSummary(message);
        var (delivered, error) = await SendToN8nAsync(message);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        db.Notifications.Add(new NotificationMessage
        {
            Id = Guid.NewGuid(),
            Type = message.Name,
            Summary = summary,
            PayloadJson = message.Payload,
            N8nDelivered = delivered,
            N8nError = error,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Stored notification {Type}. n8n delivered={Delivered}", message.Name, delivered);
    }

    private static string BuildSummary(EventMessage message)
    {
        if (message.Name == EventNames.OrderCreated)
        {
            var order = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload, Json);
            return order is null
                ? "Order created"
                : $"{order.CustomerName} ordered from {order.RestaurantName} for {order.Total:0} MMK.";
        }

        var delivery = JsonSerializer.Deserialize<DeliveryUpdatedEvent>(message.Payload, Json);
        return delivery is null
            ? "Delivery updated"
            : $"Delivery for {delivery.CustomerName} is {delivery.Status}" +
              (string.IsNullOrWhiteSpace(delivery.CourierName) ? "." : $" with {delivery.CourierName}.");
    }

    private async Task<(bool Delivered, string? Error)> SendToN8nAsync(EventMessage message)
    {
        var url = Infrastructure.Required("N8N_WEBHOOK_URL", "http://localhost:5678/webhook/minifood-order-created");
        try
        {
            var client = httpClientFactory.CreateClient("n8n");
            var response = await client.PostAsJsonAsync(url, new
            {
                type = message.Name,
                occurredAtUtc = message.OccurredAtUtc,
                payload = JsonSerializer.Deserialize<JsonElement>(message.Payload)
            });

            if (response.IsSuccessStatusCode)
                return (true, null);

            var body = await response.Content.ReadAsStringAsync();
            var error = $"n8n returned {(int)response.StatusCode}: {Trim(body)}";
            logger.LogWarning("{Error}", error);
            return (false, error);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "n8n webhook call failed. The notification is still saved.");
            return (false, Trim(ex.Message));
        }
    }

    private static string Trim(string value) => value.Length <= 480 ? value : value[..480];
}
