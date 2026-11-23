using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MiniFood.BuildingBlocks;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
var signingKey = Infrastructure.Required("JWT_SIGNING_KEY", "minifood-dev-signing-key-change-me");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = "minifood",
            ValidAudience = "minifood-web",
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

var users = new[]
{
    new DemoUser("10000000-0000-0000-0000-000000000001", "Thet", "thet@minifood.dev", "Food1234!"),
    new DemoUser("10000000-0000-0000-0000-000000000002", "Aye", "aye@minifood.dev", "Food1234!")
};

app.MapGet("/health", () => Results.Ok());
app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    var user = users.FirstOrDefault(x => string.Equals(x.Email, request.Email?.Trim(), StringComparison.OrdinalIgnoreCase));
    if (user is null || user.Password != request.Password)
        return Results.Json(new { title = "Login failed", detail = "Email or password is wrong." }, statusCode: StatusCodes.Status401Unauthorized);

    var expires = DateTime.UtcNow.AddHours(4);
    var token = new JwtSecurityToken(
        issuer: "minifood",
        audience: "minifood-web",
        claims:
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        ],
        expires: expires,
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, new UserResponse(user.Id, user.Name, user.Email)));
});

app.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
{
    var id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? principal.FindFirstValue(ClaimTypes.Email);
    var name = principal.FindFirstValue(ClaimTypes.Name);
    if (id is null || email is null || name is null)
        return Results.Unauthorized();
    return Results.Ok(new UserResponse(id, name, email));
}).RequireAuthorization();

app.Run();

internal sealed record DemoUser(string Id, string Name, string Email, string Password);
internal sealed record LoginRequest(string Email, string Password);
internal sealed record UserResponse(string Id, string Name, string Email);
internal sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
