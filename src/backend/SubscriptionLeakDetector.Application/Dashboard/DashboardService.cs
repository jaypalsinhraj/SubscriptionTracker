using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;

    public DashboardService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var subs = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);

        var likelySubs = subs
            .Where(s =>
                s.ClassificationScore >= 70 &&
                (s.RecurringType == RecurringType.SoftwareSubscription ||
                 s.RecurringType == RecurringType.MediaSubscription))
            .ToList();

        var monthlyEstimate = likelySubs.Sum(NormalizeToMonthly);
        var openAlerts = await _db.RenewalAlerts
            .AsNoTracking()
            .CountAsync(
                a => a.AccountId == accountId &&
                     a.AlertStatus != AlertStatus.Resolved &&
                     a.AlertStatus != AlertStatus.Dismissed,
                cancellationToken);

        var pendingConfirmations = await _db.RenewalAlerts
            .AsNoTracking()
            .CountAsync(a => a.AccountId == accountId && a.AlertStatus == AlertStatus.PendingConfirmation,
                cancellationToken);

        var duplicateSpend = await EstimateDuplicateSpendAsync(accountId, likelySubs, cancellationToken);

        return new DashboardSummaryDto(
            Math.Round(monthlyEstimate, 2),
            likelySubs.Count,
            openAlerts,
            pendingConfirmations,
            Math.Round(duplicateSpend, 2));
    }

    private static decimal NormalizeToMonthly(Domain.Entities.Subscription s)
    {
        return s.Cadence switch
        {
            Cadence.Weekly => s.AverageAmount * 52 / 12m,
            Cadence.Monthly => s.AverageAmount,
            Cadence.Yearly => s.AverageAmount / 12m,
            _ => s.AverageAmount
        };
    }

    private async Task<decimal> EstimateDuplicateSpendAsync(Guid accountId,
        IReadOnlyCollection<Domain.Entities.Subscription> subs,
        CancellationToken cancellationToken)
    {
        var duplicateAlerts = await _db.RenewalAlerts
            .AsNoTracking()
            .Where(a => a.AccountId == accountId && a.AlertType == AlertType.DuplicateTool)
            .Select(a => a.SubscriptionId)
            .ToListAsync(cancellationToken);

        if (duplicateAlerts.Count == 0) return 0;

        var set = duplicateAlerts.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        return subs.Where(s => set.Contains(s.Id)).Sum(NormalizeToMonthly);
    }
}
