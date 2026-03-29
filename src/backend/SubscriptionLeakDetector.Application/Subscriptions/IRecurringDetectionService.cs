namespace SubscriptionLeakDetector.Application.Subscriptions;

public interface IRecurringDetectionService
{
    Task RunForAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}
