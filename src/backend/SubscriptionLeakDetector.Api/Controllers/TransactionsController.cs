using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SubscriptionLeakDetector.Application.Alerts;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.Subscriptions;
using SubscriptionLeakDetector.Application.Transactions;
using SubscriptionLeakDetector.Api.Services;

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
    private readonly IAccountImportGate _importGate;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionImportService import,
        IRecurringDetectionService detection,
        IAlertGenerationService alerts,
        ICurrentUserService users,
        IAccountImportGate importGate,
        ILogger<TransactionsController> logger)
    {
        _import = import;
        _detection = detection;
        _alerts = alerts;
        _users = users;
        _importGate = importGate;
        _logger = logger;
    }

    [HttpPost("import")]
    public async Task<ActionResult<object>> Import([FromBody] ImportTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _users.AccountId!.Value;
        var userId = _users.UserId;

        using (await _importGate.EnterAsync(accountId, cancellationToken))
        {
            var importId = await _import.ImportCsvAsync(accountId, userId, request, cancellationToken);
            await _detection.RunForAccountAsync(accountId, cancellationToken);
            await _alerts.GenerateForAccountAsync(accountId, cancellationToken);
            _logger.LogInformation(
                "Import pipeline completed: AccountId={AccountId}, ImportId={ImportId}",
                accountId, importId);
            return Ok(new { importId });
        }
    }
}
