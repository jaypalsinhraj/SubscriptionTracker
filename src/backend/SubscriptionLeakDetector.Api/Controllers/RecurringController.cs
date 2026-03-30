using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.Recurring;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api/recurring")]
public sealed class RecurringController : ControllerBase
{
    private readonly IRecurringReviewQueryService _review;
    private readonly IRecurringCandidateActionService _actions;
    private readonly ICurrentUserService _users;

    public RecurringController(
        IRecurringReviewQueryService review,
        IRecurringCandidateActionService actions,
        ICurrentUserService users)
    {
        _review = review;
        _actions = actions;
        _users = users;
    }

    /// <summary>Borderline recurring patterns (needs review) and optionally non-subscription recurring rows.</summary>
    [HttpGet("review")]
    public async Task<ActionResult<IReadOnlyList<RecurringCandidateListItemDto>>> ListReview(
        [FromQuery] bool includeNonSubscription = false,
        CancellationToken cancellationToken = default)
    {
        var items =
            await _review.ListAsync(_users.AccountId!.Value, includeNonSubscription, cancellationToken);
        return Ok(items);
    }

    [HttpPost("candidates/{id:guid}/classify")]
    public async Task<IActionResult> Classify(Guid id, [FromBody] ClassifyRecurringCandidateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _actions.ClassifyAsync(_users.AccountId!.Value, _users.UserId!.Value, id, request,
                cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
