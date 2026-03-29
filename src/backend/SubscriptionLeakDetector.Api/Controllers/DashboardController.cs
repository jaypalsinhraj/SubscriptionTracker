using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.Dashboard;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly ICurrentUserService _users;

    public DashboardController(IDashboardService dashboard, ICurrentUserService users)
    {
        _dashboard = dashboard;
        _users = users;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken cancellationToken)
    {
        var accountId = _users.AccountId!.Value;
        var summary = await _dashboard.GetSummaryAsync(accountId, cancellationToken);
        return Ok(summary);
    }
}
