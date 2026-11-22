using Catalog.Api.Contracts;
using Catalog.Api.Features;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MiniFood.BuildingBlocks;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RestaurantDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRestaurantsQuery(), cancellationToken);
        Response.Headers["X-Cache"] = result.FromCache ? "HIT" : "MISS";
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/menu")]
    public async Task<ActionResult<MenuDto>> GetMenu(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMenuQuery(id), cancellationToken);
        if (result is null)
            return NotFound();

        Response.Headers["X-Cache"] = result.FromCache ? "HIT" : "MISS";
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<RestaurantDto>> Create(CreateRestaurantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("Name is required.");

        var created = await mediator.Send(
            new CreateRestaurantCommand(request.Name, request.Cuisine, request.Address),
            cancellationToken);
        return Created($"/api/restaurants/{created.Id}/menu", created);
    }

    [HttpPost("{id:guid}/menu")]
    public async Task<ActionResult<MenuItemDto>> AddMenuItem(Guid id, AddMenuItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppException("Name is required.");

        var created = await mediator.Send(
            new AddMenuItemCommand(id, request.Name, request.Description, request.Price),
            cancellationToken);
        return Created($"/api/restaurants/{id}/menu", created);
    }
}
