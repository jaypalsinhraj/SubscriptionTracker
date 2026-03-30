namespace SubscriptionLeakDetector.Application.Recurring;

public interface IRecurringReviewQueryService
{
    Task<IReadOnlyList<RecurringCandidateListItemDto>> ListAsync(Guid accountId, bool includeNonSubscription,
        CancellationToken cancellationToken = default);
}
