using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notification.Api.Data;

namespace Notification.Api.Controllers;

public record NotificationDto(Guid Id, string Type, string Summary, bool N8nDelivered, string? N8nError, DateTime CreatedAtUtc);

public record ListNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;

public sealed class ListNotificationsHandler(NotificationDbContext db) : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        return await db.Notifications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotificationDto(x.Id, x.Type, x.Summary, x.N8nDelivered, x.N8nError, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

[ApiController]
[Route("api/notifications")]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListNotificationsQuery(), cancellationToken);
        return Ok(items);
    }
}
