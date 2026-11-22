var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Gateway"] = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "gateway";
    await next();
});

app.MapGet("/", () => Results.Content(
    """
    <!doctype html>
    <html>
    <head><meta charset="utf-8"><title>Mini Food Delivery</title>
    <style>
      body { font-family: Georgia, serif; max-width: 42rem; margin: 3rem auto; padding: 0 1rem; color: #1c1917; background: #faf7f2; }
      code { font-family: ui-monospace, monospace; }
      a { color: #9a3412; }
    </style></head>
    <body>
      <h1>Mini Food Delivery</h1>
      <p>The gateway load-balances REST calls. Two catalog instances share one route, round robin.</p>
      <ul>
        <li><a href="/catalog/api/restaurants">GET /catalog/api/restaurants</a></li>
        <li><a href="/orders/api/orders">GET /orders/api/orders</a></li>
        <li><a href="/delivery/api/deliveries">GET /delivery/api/deliveries</a></li>
        <li><a href="/notifications/api/notifications">GET /notifications/api/notifications</a></li>
      </ul>
      <p>Scalar (OpenAPI) runs on each service: catalog 5081, ordering 5083, delivery 5084, notification 5085.</p>
      <p>Repeat the restaurants link and watch the <code>X-Service-Instance</code> response header.</p>
    </body>
    </html>
    """, "text/html"));

app.MapHealthChecks("/health");
app.MapReverseProxy();
app.Run();
