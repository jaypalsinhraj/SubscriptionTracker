using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubscriptionLeakDetector.Api.Options;
using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api")]
public sealed class MeController : ControllerBase
{
    private readonly ICurrentUserService _users;
    private readonly IApplicationDbContext _db;
    private readonly GlobalizationOptions _globalFallback;

    public MeController(
        ICurrentUserService users,
        IApplicationDbContext db,
        IOptions<GlobalizationOptions> globalization)
    {
        _users = users;
        _db = db;
        _globalFallback = globalization.Value;
    }

    [HttpGet("me")]
    public async Task<ActionResult<object>> GetMe(CancellationToken cancellationToken)
    {
        var accountId = _users.AccountId!.Value;
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        var uiCulture = string.IsNullOrWhiteSpace(account?.UiCulture)
            ? _globalFallback.DefaultCulture
            : account!.UiCulture.Trim();

        var defaultCurrency = string.IsNullOrWhiteSpace(account?.DefaultCurrency)
            ? _globalFallback.DefaultCurrency.Trim().ToUpperInvariant()
            : account!.DefaultCurrency.Trim().ToUpperInvariant();

        return Ok(new
        {
            userId = _users.UserId,
            accountId = _users.AccountId,
            email = _users.Email,
            uiCulture,
            defaultCurrency
        });
    }
}
