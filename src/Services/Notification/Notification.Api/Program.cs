using Microsoft.EntityFrameworkCore;
using MiniFood.BuildingBlocks;
using Notification.Api.Data;
using Notification.Api.Workers;
using Scalar.AspNetCore;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(Infrastructure.Sql("MiniFoodNotification"), sql => sql.EnableRetryOnFailure()));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHttpClient("n8n", client => client.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddControllers();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Notification API";
        document.Info.Description = "Listens to Redis pub/sub (async) and forwards each event to an n8n webhook. The row is saved even when n8n is offline.";
        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NotificationStartup");
    await DatabaseStartup.EnsureCreatedAsync(db, logger);
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Service-Instance"] = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "notification";
    await next();
});
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Notification API"));
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/scalar"));
app.Run();
