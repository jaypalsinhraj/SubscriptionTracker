using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionLeakDetector.Application.Alerts;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.Subscriptions;
using SubscriptionLeakDetector.Application.Transactions;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionImportService _import;
    private readonly IRecurringDetectionService _detection;
    private readonly IAlertGenerationService _alerts;
    private readonly ICurrentUserService _users;

    public TransactionsController(
        ITransactionImportService import,
        IRecurringDetectionService detection,
        IAlertGenerationService alerts,
        ICurrentUserService users)
    {
        _import = import;
        _detection = detection;
        _alerts = alerts;
        _users = users;
    }

    [HttpPost("import")]
    public async Task<ActionResult<object>> Import([FromBody] ImportTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _users.AccountId!.Value;
        var userId = _users.UserId;
        var importId = await _import.ImportCsvAsync(accountId, userId, request, cancellationToken);
        await _detection.RunForAccountAsync(accountId, cancellationToken);
        await _alerts.GenerateForAccountAsync(accountId, cancellationToken);
        return Ok(new { importId });
    }
}
