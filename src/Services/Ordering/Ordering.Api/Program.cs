using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;
using MiniFood.BuildingBlocks.Grpc;
using Ordering.Api.Data;
using Scalar.AspNetCore;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
});

builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseSqlServer(Infrastructure.Sql("MiniFoodOrdering"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddMiniFoodRedis();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(options =>
{
    options.Address = new Uri(Infrastructure.Required("GRPC_CATALOG", "http://localhost:5081"));
});
builder.Services.AddGrpcClient<DeliveryGrpc.DeliveryGrpcClient>(options =>
{
    options.Address = new Uri(Infrastructure.Required("GRPC_DELIVERY", "http://localhost:5084"));
});
builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Ordering API";
        document.Info.Description = "Place an order. The handler calls Catalog over gRPC (sync) for the price, saves SQL, then publishes order.created on Redis (async). GET order calls Delivery over gRPC (sync) for courier status.";
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OrderingExceptionHandler>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OrderingStartup");
    await DatabaseStartup.EnsureCreatedAsync(db, logger);
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Service-Instance"] = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "ordering";
    await next();
});
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Ordering API"));
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/scalar"));
app.Run();

public sealed class OrderingExceptionHandler(IProblemDetailsService problemDetails) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException app)
            return false;

        httpContext.Response.StatusCode = app.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = "Request failed",
                Detail = app.Message,
                Status = app.StatusCode
            }
        });
    }
}
