using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public class SubscriptionQueryService : ISubscriptionQueryService
{
    private readonly IApplicationDbContext _db;

    public SubscriptionQueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(Guid accountId, bool includeReviewBucket,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Subscriptions.AsNoTracking().Where(s => s.AccountId == accountId);

        if (includeReviewBucket)
            q = q.Where(s =>
                (s.ClassificationScore >= 70 &&
                 (s.RecurringType == RecurringType.SoftwareSubscription ||
                  s.RecurringType == RecurringType.MediaSubscription)) ||
                (s.ClassificationScore >= 40 && s.ClassificationScore < 70 &&
                 (s.RecurringType == RecurringType.SoftwareSubscription ||
                  s.RecurringType == RecurringType.MediaSubscription ||
                  s.RecurringType == RecurringType.UnknownRecurring)));
        else
            q = q.Where(s =>
                s.ClassificationScore >= 70 &&
                (s.RecurringType == RecurringType.SoftwareSubscription ||
                 s.RecurringType == RecurringType.MediaSubscription));

        return await q
            .OrderBy(s => s.VendorName)
            .Select(s => new SubscriptionListItemDto(
                s.Id,
                s.VendorName,
                s.NormalizedMerchant,
                s.RecurringType,
                s.ClassificationScore,
                s.ClassificationReason,
                s.AverageAmount,
                s.Currency,
                s.Cadence,
                s.LastChargeDate,
                s.NextExpectedChargeDate,
                s.Status,
                s.ConfidenceScore,
                s.OwnerUserId,
                s.OwnerName,
                s.OwnerEmail,
                s.ReviewStatus,
                s.LastConfirmedInUseAt,
                s.LastReviewRequestedAt,
                s.NextReviewDate,
                s.UsageConfidenceScore))
            .ToListAsync(cancellationToken);
    }
}
