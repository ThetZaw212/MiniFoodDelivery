using Ordering.Api.Contracts;
using Ordering.Api.Data;

namespace Ordering.Api.Features;

public static class OrderMapper
{
    public static OrderDto ToDto(CustomerOrder order, string deliveryStatus, string? courierName) =>
        new(
            order.Id,
            order.RestaurantId,
            order.RestaurantName,
            order.CustomerName,
            order.DeliveryAddress,
            order.Status,
            order.Total,
            order.CreatedAtUtc,
            deliveryStatus,
            courierName,
            order.Lines.Select(x => new OrderLineDto(x.MenuItemId, x.Name, x.UnitPrice, x.Quantity)).ToList());
}
