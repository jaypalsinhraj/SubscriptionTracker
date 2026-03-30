using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionLeakDetector.Application.Accounts;
using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountDataResetService _reset;
    private readonly ICurrentUserService _users;

    public AccountController(IAccountDataResetService reset, ICurrentUserService users)
    {
        _reset = reset;
        _users = users;
    }

    /// <summary>Deletes all transactions, imports, subscriptions, recurring candidates, alerts, audit logs, and bank connections for the current account. Account and users are kept.</summary>
    [HttpPost("reset-data")]
    public async Task<IActionResult> ResetData([FromBody] ResetAccountDataRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirm)
            return BadRequest(new { error = "Set confirm to true to delete all account data." });

        await _reset.ResetAllDataAsync(_users.AccountId!.Value, cancellationToken);
        return NoContent();
    }
}
