using Catalog.Api;
using Catalog.Api.Data;
using Catalog.Api.Grpc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;
using Scalar.AspNetCore;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    // REST stays on the normal HTTP port. gRPC needs its own HTTP/2 port when there is no TLS.
    if (int.TryParse(Environment.GetEnvironmentVariable("GRPC_LISTEN_PORT"), out var grpcPort))
    {
        var httpPort = int.TryParse(Environment.GetEnvironmentVariable("HTTP_LISTEN_PORT"), out var configured) ? configured : 8080;
        options.ListenAnyIP(httpPort, listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
        options.ListenAnyIP(grpcPort, listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
    }
});

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(Infrastructure.Sql("MiniFoodCatalog"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = Infrastructure.Redis);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Catalog API";
        document.Info.Description = "Restaurants and menus. Queries use CQRS. Restaurant lists are cached in Redis for 30 seconds. gRPC GetMenuItem is the synchronous read used by Ordering.";
        return Task.CompletedTask;
    });
});
builder.Services.AddGrpc();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CatalogStartup");
    await DatabaseStartup.EnsureCreatedAsync(db, logger, async () =>
    {
        await CatalogSeed.SeedAsync(db);
        await CatalogSeed.EnsureStockColumnAsync(db);
    });
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Service-Instance"] = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "catalog";
    await next();
});
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Catalog API"));
app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/scalar"));
app.Run();

public sealed class AppExceptionHandler(IProblemDetailsService problemDetails) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
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
