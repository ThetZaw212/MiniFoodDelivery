namespace MiniFood.BuildingBlocks;

public record EventMessage(string Name, string Payload, DateTime OccurredAtUtc);

public record OrderCreatedEvent(
    Guid OrderId,
    Guid RestaurantId,
    string RestaurantName,
    string CustomerName,
    string DeliveryAddress,
    decimal Total,
    DateTime CreatedAtUtc);

public record DeliveryUpdatedEvent(
    Guid DeliveryId,
    Guid OrderId,
    string Status,
    string? CourierName,
    string CustomerName,
    string Address,
    DateTime OccurredAtUtc);
