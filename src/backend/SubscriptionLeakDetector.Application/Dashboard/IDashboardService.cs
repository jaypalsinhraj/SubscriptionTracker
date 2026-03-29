namespace SubscriptionLeakDetector.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid accountId, CancellationToken cancellationToken = default);
}
