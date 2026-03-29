namespace SubscriptionLeakDetector.Application.Subscriptions;

public interface ISubscriptionQueryService
{
    /// <param name="includeReviewBucket">When true, include 40–69 score review candidates (software/media/unknown).</param>
    Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(Guid accountId, bool includeReviewBucket = false,
        CancellationToken cancellationToken = default);
}
