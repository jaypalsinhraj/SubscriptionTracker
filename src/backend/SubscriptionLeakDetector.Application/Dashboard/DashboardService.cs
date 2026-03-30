using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Classification;
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
                s.SubscriptionConfidenceScore >= 70 &&
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

        // Derive exposure from current subscriptions using the same grouping rules as duplicate-tool alerts.
        // Do not rely on renewal_alerts rows: SubscriptionId is set null when subscriptions are recreated (FK SetNull).
        var duplicateSpend = EstimateDuplicateMonthlySpend(subs);

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
            Cadence.Quarterly => s.AverageAmount / 3m,
            Cadence.Yearly => s.AverageAmount / 12m,
            _ => s.AverageAmount
        };
    }

    /// <summary>
    /// Estimated monthly spend for subscriptions that share a normalized merchant key with at least one other
    /// eligible row (same grouping as duplicate-tool alert generation).
    /// </summary>
    private static decimal EstimateDuplicateMonthlySpend(IReadOnlyCollection<Domain.Entities.Subscription> activeSubscriptions)
    {
        var dupCandidates = activeSubscriptions
            .Where(s => RecurringClassifier.IncludedInDuplicateDetection(s.RecurringType, s.SubscriptionConfidenceScore))
            .ToList();

        var groups = dupCandidates
            .GroupBy(x => string.IsNullOrWhiteSpace(x.NormalizedMerchant)
                ? NormalizeMerchantKey(x.VendorName)
                : x.NormalizedMerchant)
            .Where(g => g.Count() > 1);

        decimal total = 0;
        foreach (var g in groups)
        {
            foreach (var s in g)
                total += NormalizeToMonthly(s);
        }

        return total;
    }

    private static string NormalizeMerchantKey(string name) =>
        string.Join(' ', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
