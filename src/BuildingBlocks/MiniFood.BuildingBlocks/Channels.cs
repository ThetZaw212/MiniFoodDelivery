namespace MiniFood.BuildingBlocks;

public static class Channels
{
    public const string Orders = "minifood.orders";
    public const string Deliveries = "minifood.deliveries";
}

public static class EventNames
{
    public const string OrderCreated = "order.created";
    public const string DeliveryUpdated = "delivery.updated";
}
