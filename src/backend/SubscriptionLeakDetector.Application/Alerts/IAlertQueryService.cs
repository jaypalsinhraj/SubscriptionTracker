namespace SubscriptionLeakDetector.Application.Alerts;

public interface IAlertQueryService
{
    Task<IReadOnlyList<AlertListItemDto>> ListAsync(Guid accountId, CancellationToken cancellationToken = default);
}
