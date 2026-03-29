using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Infrastructure.Persistence;

namespace SubscriptionLeakDetector.Api.Middleware;

public sealed class UserResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public UserResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var oid = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("oid")
                  ?? context.User.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(oid))
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ExternalId == oid);
            if (user != null)
            {
                context.Items["UserId"] = user.Id;
                context.Items["AccountId"] = user.AccountId;
                context.Items["UserEmail"] = user.Email;
            }
        }

        await _next(context);
    }
}
