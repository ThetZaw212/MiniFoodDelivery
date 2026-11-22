using Delivery.Api.Data;
using Delivery.Api.Grpc;
using Delivery.Api.Workers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;
using Scalar.AspNetCore;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    if (int.TryParse(Environment.GetEnvironmentVariable("GRPC_LISTEN_PORT"), out var grpcPort))
    {
        var httpPort = int.TryParse(Environment.GetEnvironmentVariable("HTTP_LISTEN_PORT"), out var configured) ? configured : 8080;
        options.ListenAnyIP(httpPort, listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
        options.ListenAnyIP(grpcPort, listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
    }
});

builder.Services.AddDbContext<DeliveryDbContext>(options =>
    options.UseSqlServer(Infrastructure.Sql("MiniFoodCourier"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddMiniFoodRedis();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddHostedService<OrderCreatedWorker>();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Delivery API";
        document.Info.Description = "Creates a delivery when it receives order.created from Redis pub/sub (async). Exposes GetByOrderId over gRPC so Ordering can read courier status (sync).";
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DeliveryExceptionHandler>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DeliveryStartup");
    await DatabaseStartup.EnsureCreatedAsync(db, logger);
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Service-Instance"] = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "delivery";
    await next();
});
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Delivery API"));
app.MapControllers();
app.MapGrpcService<DeliveryGrpcService>();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/scalar"));
app.Run();

public sealed class DeliveryExceptionHandler(IProblemDetailsService problemDetails) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException app)
            return false;

        httpContext.Response.StatusCode = app.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Title = "Request failed", Detail = app.Message, Status = app.StatusCode }
        });
    }
}
