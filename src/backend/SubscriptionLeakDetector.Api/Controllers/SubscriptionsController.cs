using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.Subscriptions;

namespace SubscriptionLeakDetector.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountMember")]
[Route("api/subscriptions")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionQueryService _subscriptions;
    private readonly ISubscriptionReviewService _review;
    private readonly ICurrentUserService _users;

    public SubscriptionsController(
        ISubscriptionQueryService subscriptions,
        ISubscriptionReviewService review,
        ICurrentUserService users)
    {
        _subscriptions = subscriptions;
        _review = review;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionListItemDto>>> List(
        [FromQuery] bool likelySaaSMediaOnly = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _subscriptions.ListAsync(_users.AccountId!.Value, likelySaaSMediaOnly, cancellationToken);
        return Ok(items);
    }

    [HttpPatch("{id:guid}/owner")]
    public async Task<ActionResult<SubscriptionListItemDto>> PatchOwner(Guid id, [FromBody] AssignOwnerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _review.AssignOwnerAsync(_users.AccountId!.Value, _users.UserId!.Value, id, request,
                cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/request-review")]
    public async Task<IActionResult> RequestReview(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _review.RequestReviewAsync(_users.AccountId!.Value, _users.UserId!.Value, id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
