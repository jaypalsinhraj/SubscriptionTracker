namespace SubscriptionLeakDetector.Application.Subscriptions;

public interface ISubscriptionQueryService
{
    /// <param name="likelySaaSMediaOnly">
    /// When true, returns only software + media types with confidence ≥70 (legacy dashboard-style filter).
    /// When false, returns all active subscriptions (full list).
    /// </param>
    Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(Guid accountId, bool likelySaaSMediaOnly = false,
        CancellationToken cancellationToken = default);
}
