using Delivery.Api.Contracts;
using Delivery.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;

namespace Delivery.Api.Features;

public record ListDeliveriesQuery : IRequest<IReadOnlyList<DeliveryDto>>;

public record GetDeliveryQuery(Guid Id) : IRequest<DeliveryDto?>;

public record AssignCourierCommand(Guid Id, string CourierName) : IRequest<DeliveryDto>;

public record UpdateDeliveryStatusCommand(Guid Id, string Status) : IRequest<DeliveryDto>;

public sealed class ListDeliveriesHandler(DeliveryDbContext db) : IRequestHandler<ListDeliveriesQuery, IReadOnlyList<DeliveryDto>>
{
    public async Task<IReadOnlyList<DeliveryDto>> Handle(ListDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var jobs = await db.Deliveries.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return jobs.Select(DeliveryMapper.ToDto).ToList();
    }
}

public sealed class GetDeliveryHandler(DeliveryDbContext db) : IRequestHandler<GetDeliveryQuery, DeliveryDto?>
{
    public async Task<DeliveryDto?> Handle(GetDeliveryQuery request, CancellationToken cancellationToken)
    {
        var job = await db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        return job is null ? null : DeliveryMapper.ToDto(job);
    }
}

public sealed class AssignCourierHandler(DeliveryDbContext db, IEventBus bus) : IRequestHandler<AssignCourierCommand, DeliveryDto>
{
    public async Task<DeliveryDto> Handle(AssignCourierCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CourierName))
            throw new AppException("Courier name is required.");

        var job = await db.Deliveries.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (job is null)
            throw new AppException("Delivery was not found.", StatusCodes.Status404NotFound);

        job.CourierName = request.CourierName.Trim();
        job.Status = "Assigned";
        await db.SaveChangesAsync(cancellationToken);
        await PublishAsync(bus, job, cancellationToken);
        return DeliveryMapper.ToDto(job);
    }

    internal static Task PublishAsync(IEventBus bus, DeliveryJob job, CancellationToken cancellationToken) =>
        bus.PublishAsync(Channels.Deliveries, EventNames.DeliveryUpdated, new DeliveryUpdatedEvent(
            job.Id,
            job.OrderId,
            job.Status,
            job.CourierName,
            job.CustomerName,
            job.Address,
            DateTime.UtcNow), cancellationToken);
}

public sealed class UpdateDeliveryStatusHandler(DeliveryDbContext db, IEventBus bus) : IRequestHandler<UpdateDeliveryStatusCommand, DeliveryDto>
{
    private static readonly HashSet<string> Allowed = ["WaitingForCourier", "Assigned", "PickedUp", "Delivered"];

    public async Task<DeliveryDto> Handle(UpdateDeliveryStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Allowed.Contains(request.Status))
            throw new AppException($"Status must be one of: {string.Join(", ", Allowed)}.");

        var job = await db.Deliveries.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (job is null)
            throw new AppException("Delivery was not found.", StatusCodes.Status404NotFound);

        job.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        await AssignCourierHandler.PublishAsync(bus, job, cancellationToken);
        return DeliveryMapper.ToDto(job);
    }
}

public static class DeliveryMapper
{
    public static DeliveryDto ToDto(DeliveryJob job) =>
        new(job.Id, job.OrderId, job.RestaurantName, job.CustomerName, job.Address, job.Status, job.CourierName, job.CreatedAtUtc);
}
