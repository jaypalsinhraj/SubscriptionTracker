using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Recurring;

public sealed class RecurringReviewQueryService : IRecurringReviewQueryService
{
    private readonly IApplicationDbContext _db;

    public RecurringReviewQueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecurringCandidateListItemDto>> ListAsync(Guid accountId, bool includeNonSubscription,
        CancellationToken cancellationToken = default)
    {
        var q = _db.RecurringCandidates.AsNoTracking().Where(c => c.AccountId == accountId);
        if (!includeNonSubscription) q = q.Where(c => c.Status == RecurringCandidateStatus.NeedsReview);

        var rows = await q.OrderByDescending(c => c.SubscriptionConfidenceScore).ThenBy(c => c.VendorName)
            .ToListAsync(cancellationToken);

        return rows.Select(c => new RecurringCandidateListItemDto(
            c.Id,
            c.VendorName,
            c.NormalizedMerchant,
            c.RecurringType,
            c.SubscriptionConfidenceScore,
            c.ClassificationReason,
            c.PatternConfidenceScore,
            c.Cadence,
            c.AverageAmount,
            c.Currency,
            c.LastChargeDate,
            c.NextExpectedChargeDate,
            c.Status,
            MapLabel(c))).ToList();
    }

    private static string MapLabel(RecurringCandidate c)
    {
        if (c.Status == RecurringCandidateStatus.NeedsReview) return "Needs review";
        return c.RecurringType switch
        {
            RecurringType.UtilityBill => "Recurring utility",
            RecurringType.Salary or RecurringType.RecurringIncome => "Recurring income",
            RecurringType.Transfer => "Transfer",
            RecurringType.Telecom => "Telecom",
            RecurringType.Rent => "Rent",
            _ => "Recurring (non-subscription)"
        };
    }
}
