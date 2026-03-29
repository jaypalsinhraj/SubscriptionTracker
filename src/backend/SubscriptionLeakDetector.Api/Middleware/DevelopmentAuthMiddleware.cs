using System.Security.Claims;

namespace SubscriptionLeakDetector.Api.Middleware;

/// <summary>
/// When enabled, assigns a synthetic principal for local development without Entra tokens.
/// </summary>
public sealed class DevelopmentAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DevelopmentAuthMiddleware(RequestDelegate next, IWebHostEnvironment env, IConfiguration config)
    {
        _next = next;
        _env = env;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = _config.GetValue("Auth:DevelopmentBypass:Enabled", false);
        if (_env.IsDevelopment() && enabled && context.User.Identity?.IsAuthenticated != true)
        {
            var externalId = _config["Auth:DevelopmentBypass:ExternalUserId"] ?? "dev-user";
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, externalId),
                new Claim("oid", externalId)
            };
            var id = new ClaimsIdentity(claims, "Development");
            context.User = new ClaimsPrincipal(id);
        }

        await _next(context);
    }
}
