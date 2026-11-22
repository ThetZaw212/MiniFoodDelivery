namespace Delivery.Api.Contracts;

public record DeliveryDto(
    Guid Id,
    Guid OrderId,
    string RestaurantName,
    string CustomerName,
    string Address,
    string Status,
    string? CourierName,
    DateTime CreatedAtUtc);

public record AssignCourierRequest(string CourierName);

public record UpdateDeliveryStatusRequest(string Status);
