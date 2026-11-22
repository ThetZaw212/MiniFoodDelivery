using Delivery.Api.Contracts;
using Delivery.Api.Features;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Delivery.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
public class DeliveriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeliveryDto>>> List(CancellationToken cancellationToken)
    {
        var jobs = await mediator.Send(new ListDeliveriesQuery(), cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new GetDeliveryQuery(id), cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<DeliveryDto>> Assign(Guid id, AssignCourierRequest request, CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new AssignCourierCommand(id, request.CourierName), cancellationToken);
        return Ok(job);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<DeliveryDto>> UpdateStatus(Guid id, UpdateDeliveryStatusRequest request, CancellationToken cancellationToken)
    {
        var job = await mediator.Send(new UpdateDeliveryStatusCommand(id, request.Status), cancellationToken);
        return Ok(job);
    }
}
