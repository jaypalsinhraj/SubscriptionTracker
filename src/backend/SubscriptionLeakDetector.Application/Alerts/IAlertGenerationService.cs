namespace SubscriptionLeakDetector.Application.Alerts;

public interface IAlertGenerationService
{
    Task GenerateForAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}
