namespace SubscriptionLeakDetector.Application.Subscriptions;

public interface ISubscriptionReviewService
{
    Task<SubscriptionListItemDto?> AssignOwnerAsync(Guid accountId, Guid actingUserId, Guid subscriptionId,
        AssignOwnerRequest request, CancellationToken cancellationToken = default);

    Task RequestReviewAsync(Guid accountId, Guid actingUserId, Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<AlertRespondResultDto> RespondToAlertAsync(Guid accountId, Guid actingUserId, Guid alertId,
        RespondToAlertRequest request, CancellationToken cancellationToken = default);
}
