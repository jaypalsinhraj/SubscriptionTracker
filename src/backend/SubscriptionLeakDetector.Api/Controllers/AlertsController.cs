using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionLeakDetector.Application.Alerts;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.Subscriptions;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertQueryService _alerts;
    private readonly ISubscriptionReviewService _review;
    private readonly ICurrentUserService _users;

    public AlertsController(
        IAlertQueryService alerts,
        ISubscriptionReviewService review,
        ICurrentUserService users)
    {
        _alerts = alerts;
        _review = review;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertListItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await _alerts.ListAsync(_users.AccountId!.Value, cancellationToken);
        return Ok(items);
    }

    [HttpPost("{id:guid}/respond")]
    public async Task<ActionResult<AlertRespondResultDto>> Respond(Guid id, [FromBody] RespondToAlertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _review.RespondToAlertAsync(_users.AccountId!.Value, _users.UserId!.Value, id, request,
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
