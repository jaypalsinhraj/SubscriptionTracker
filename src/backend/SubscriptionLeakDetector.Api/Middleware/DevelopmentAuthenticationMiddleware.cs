using System.Security.Claims;

namespace SubscriptionLeakDetector.Api.Middleware;

public sealed class DevelopmentAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public DevelopmentAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config, IWebHostEnvironment env)
    {
        var enabled = config.GetValue("Auth:DevelopmentAuth:Enabled", false);
        if (env.IsDevelopment() && enabled && context.User.Identity?.IsAuthenticated != true)
        {
            var oid = config["Auth:DevelopmentAuth:UserObjectId"] ?? "dev-user";
            var email = config["Auth:DevelopmentAuth:Email"] ?? "dev@local.test";
            var claims = new[]
            {
                new Claim("oid", oid),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, oid)
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Development"));
        }

        await _next(context);
    }
}
