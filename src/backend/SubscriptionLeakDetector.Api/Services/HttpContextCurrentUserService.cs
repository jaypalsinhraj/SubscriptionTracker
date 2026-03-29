using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Api.Services;

public sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public HttpContextCurrentUserService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid? UserId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Items.TryGetValue("UserId", out var v) == true && v is Guid g) return g;
            return null;
        }
    }

    public Guid? AccountId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.Items.TryGetValue("AccountId", out var v) == true && v is Guid g) return g;
            return null;
        }
    }

    public string? Email
    {
        get
        {
            var ctx = _http.HttpContext;
            return ctx?.Items["UserEmail"] as string;
        }
    }

    public bool IsAuthenticated => UserId.HasValue && AccountId.HasValue;
}
