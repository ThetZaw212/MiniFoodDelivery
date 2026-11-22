using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Api.Contracts;
using Ordering.Api.Features;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> List([FromQuery] string? customer, CancellationToken cancellationToken)
    {
        var orders = await mediator.Send(new ListOrdersQuery(customer), cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var order = await mediator.Send(new GetOrderQuery(id), cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var created = await mediator.Send(
            new CreateOrderCommand(request.RestaurantId, request.CustomerName, request.DeliveryAddress, request.Lines ?? []),
            cancellationToken);
        return Created($"/api/orders/{created.Id}", created);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await mediator.Send(new UpdateOrderStatusCommand(id, request.Status), cancellationToken);
        return Ok(updated);
    }
}
