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

    public async Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(Guid accountId, bool likelySaaSMediaOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking()
            .Where(s => s.AccountId == accountId && s.Status == SubscriptionStatus.Active);

        if (likelySaaSMediaOnly)
        {
            query = query.Where(s =>
                s.SubscriptionConfidenceScore >= 70 &&
                (s.RecurringType == RecurringType.SoftwareSubscription ||
                 s.RecurringType == RecurringType.MediaSubscription));
        }

        return await query
            .OrderBy(s => s.VendorName)
            .Select(s => new SubscriptionListItemDto(
                s.Id,
                s.VendorName,
                s.NormalizedMerchant,
                s.RecurringType,
                s.SubscriptionConfidenceScore,
                s.ClassificationReason,
                s.IsSubscriptionCandidate,
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
