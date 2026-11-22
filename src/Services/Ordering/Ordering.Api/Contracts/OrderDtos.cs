namespace Ordering.Api.Contracts;

public record OrderLineRequest(Guid MenuItemId, int Quantity);

public record CreateOrderRequest(
    Guid RestaurantId,
    string CustomerName,
    string DeliveryAddress,
    List<OrderLineRequest> Lines);

public record UpdateOrderStatusRequest(string Status);

public record OrderLineDto(Guid MenuItemId, string Name, decimal UnitPrice, int Quantity);

public record OrderDto(
    Guid Id,
    Guid RestaurantId,
    string RestaurantName,
    string CustomerName,
    string DeliveryAddress,
    string Status,
    decimal Total,
    DateTime CreatedAtUtc,
    string DeliveryStatus,
    string? CourierName,
    IReadOnlyList<OrderLineDto> Lines);
