using Delivery.Api.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks.Grpc;

namespace Delivery.Api.Grpc;

public class DeliveryGrpcService(DeliveryDbContext db, ILogger<DeliveryGrpcService> logger) : DeliveryGrpc.DeliveryGrpcBase
{
    public override async Task<GetByOrderIdReply> GetByOrderId(GetByOrderIdRequest request, ServerCallContext context)
    {
        logger.LogInformation("Sync gRPC GetByOrderId {OrderId}", request.OrderId);

        if (!Guid.TryParse(request.OrderId, out var orderId))
            return new GetByOrderIdReply { Found = false };

        var job = await db.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, context.CancellationToken);
        if (job is null)
            return new GetByOrderIdReply { Found = false };

        return new GetByOrderIdReply
        {
            Found = true,
            DeliveryId = job.Id.ToString(),
            OrderId = job.OrderId.ToString(),
            Status = job.Status,
            CourierName = job.CourierName ?? ""
        };
    }
}
