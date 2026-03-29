using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Classification;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Alerts;

public class AlertGenerationService : IAlertGenerationService
{
    private const int RenewalWindowDays = 14;
    private const int UnusedLookbackDays = 90;
    private const int ConfirmationTtlDays = 90;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AlertGenerationService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task GenerateForAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var today = _clock.TodayUtc;

        var dupes = await _db.RenewalAlerts
            .Where(a => a.AccountId == accountId && a.AlertType == AlertType.DuplicateTool)
            .ToListAsync(cancellationToken);
        _db.RenewalAlerts.RemoveRange(dupes);

        var allSubs = await _db.Subscriptions
            .Where(s => s.AccountId == accountId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);

        var subs = allSubs
            .Where(s => RecurringClassifier.IsEligibleForSubscriptionAlerts(s.RecurringType, s.ClassificationScore))
            .ToList();

        var activeBlocks = await _db.RenewalAlerts.AsNoTracking()
            .Where(a => a.AccountId == accountId &&
                        a.SubscriptionId != null &&
                        (a.AlertStatus == AlertStatus.Open || a.AlertStatus == AlertStatus.PendingConfirmation) &&
                        (a.AlertType == AlertType.RenewalApproaching ||
                         a.AlertType == AlertType.SuspectedUnused ||
                         a.AlertType == AlertType.OwnerMissing ||
                         a.AlertType == AlertType.OwnerConfirmationRequest))
            .Select(a => new { a.SubscriptionId, a.AlertType })
            .ToListAsync(cancellationToken);

        var blocking = activeBlocks
            .Where(x => x.SubscriptionId != null)
            .Select(x => (x.SubscriptionId!.Value, x.AlertType))
            .ToHashSet();

        foreach (var s in subs)
        {
            var daysToRenewal = s.NextExpectedChargeDate.DayNumber - today.DayNumber;
            var inRenewalWindow = daysToRenewal >= 0 && daysToRenewal <= RenewalWindowDays;

            var lastActivity = await _db.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId &&
                            !t.IsCredit &&
                            t.VendorName.ToLower() == s.VendorName.ToLower())
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => t.TransactionDate)
                .FirstOrDefaultAsync(cancellationToken);

            var suspectedUnused = lastActivity != default &&
                                  lastActivity < today.AddDays(-UnusedLookbackDays);

            var renewalDue = inRenewalWindow && ReviewCycleAllowsPrompt(s, today);
            var unusedDue = suspectedUnused && RecentlyConfirmedAllowsUnusedPrompt(s, now);

            if (renewalDue && !blocking.Contains((s.Id, AlertType.RenewalApproaching)))
            {
                _db.RenewalAlerts.Add(CreateAlert(accountId, s.Id, AlertType.RenewalApproaching,
                    daysToRenewal <= 3 ? AlertSeverity.Warning : AlertSeverity.Info,
                    $"Renewal approaching — confirmation requested: {s.VendorName}",
                    $"Next charge around {s.NextExpectedChargeDate:yyyy-MM-dd} (~{daysToRenewal} days). Please confirm this subscription is still needed.",
                    AlertStatus.PendingConfirmation, now));
                blocking.Add((s.Id, AlertType.RenewalApproaching));
                if (s.ReviewStatus == ReviewStatus.None) s.ReviewStatus = ReviewStatus.NeedsReview;
            }

            if (unusedDue && !blocking.Contains((s.Id, AlertType.SuspectedUnused)))
            {
                _db.RenewalAlerts.Add(CreateAlert(accountId, s.Id, AlertType.SuspectedUnused,
                    AlertSeverity.Warning,
                    $"Suspected unused — confirmation requested: {s.VendorName}",
                    $"No matching transactions in the last {UnusedLookbackDays} days. Bank data alone cannot prove cancellation — please confirm.",
                    AlertStatus.PendingConfirmation, now));
                blocking.Add((s.Id, AlertType.SuspectedUnused));
                if (s.ReviewStatus == ReviewStatus.None) s.ReviewStatus = ReviewStatus.NeedsReview;
            }

            var noOwner = s.OwnerUserId == null && string.IsNullOrWhiteSpace(s.OwnerName) &&
                          string.IsNullOrWhiteSpace(s.OwnerEmail);
            var wantsOwnerHint = (inRenewalWindow && ReviewCycleAllowsPrompt(s, today)) ||
                                 (suspectedUnused && RecentlyConfirmedAllowsUnusedPrompt(s, now));
            if (noOwner && wantsOwnerHint && !blocking.Contains((s.Id, AlertType.OwnerMissing)))
            {
                _db.RenewalAlerts.Add(CreateAlert(accountId, s.Id, AlertType.OwnerMissing,
                    AlertSeverity.Warning,
                    $"Owner missing: {s.VendorName}",
                    "Assign an owner so confirmations route to the right person.",
                    AlertStatus.PendingConfirmation, now));
                blocking.Add((s.Id, AlertType.OwnerMissing));
            }
        }

        var dupCandidates = allSubs
            .Where(s => RecurringClassifier.IncludedInDuplicateDetection(s.RecurringType, s.ClassificationScore))
            .ToList();

        var normalized = dupCandidates
            .GroupBy(x => Normalize(x.VendorName))
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var g in normalized)
        {
            foreach (var sub in g)
            {
                _db.RenewalAlerts.Add(CreateAlert(accountId, sub.Id, AlertType.DuplicateTool,
                    AlertSeverity.Info,
                    $"Duplicate tools: {sub.VendorName}",
                    "Multiple recurring charges detected for a similar vendor name.",
                    AlertStatus.Open, now));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool ReviewCycleAllowsPrompt(Domain.Entities.Subscription s, DateOnly today) =>
        !s.NextReviewDate.HasValue || s.NextReviewDate.Value <= today;

    private bool RecentlyConfirmedAllowsUnusedPrompt(Domain.Entities.Subscription s, DateTimeOffset now)
    {
        if (s.LastConfirmedInUseAt.HasValue &&
            s.LastConfirmedInUseAt.Value.AddDays(ConfirmationTtlDays) > now)
            return false;
        return ReviewCycleAllowsPrompt(s, _clock.TodayUtc);
    }

    private static RenewalAlert CreateAlert(Guid accountId, Guid subscriptionId, AlertType type,
        AlertSeverity severity, string title, string message, AlertStatus status, DateTimeOffset now)
    {
        return new RenewalAlert
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            SubscriptionId = subscriptionId,
            AlertType = type,
            Severity = severity,
            Title = title,
            Message = message,
            IsRead = false,
            AlertStatus = status,
            CreatedAt = now
        };
    }

    private static string Normalize(string name) =>
        string.Join(' ', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
