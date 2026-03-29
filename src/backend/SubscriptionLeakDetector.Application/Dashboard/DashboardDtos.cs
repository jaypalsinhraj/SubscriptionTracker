namespace SubscriptionLeakDetector.Application.Dashboard;

public record DashboardSummaryDto(
    decimal MonthlySaaSSpendEstimate,
    int ActiveSubscriptionCount,
    int OpenAlertCount,
    int PendingConfirmationCount,
    decimal PotentialDuplicateSpend);
