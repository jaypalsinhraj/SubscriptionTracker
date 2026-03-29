using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Classification;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public class RecurringDetectionService : IRecurringDetectionService
{
    private const decimal AmountToleranceRatio = 0.08m;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public RecurringDetectionService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task RunForAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var txs = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId && !t.IsCredit)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        var groups = txs
            .GroupBy(t => NormalizeVendor(t.VendorName))
            .Where(g => g.Count() >= 2)
            .ToList();

        var existing = await _db.Subscriptions.Where(s => s.AccountId == accountId).ToListAsync(cancellationToken);
        _db.Subscriptions.RemoveRange(existing);

        var now = _clock.UtcNow;
        var today = _clock.TodayUtc;

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(t => t.TransactionDate).ToList();
            if (!TryDetect(ordered, today, out var detected)) continue;

            var vendorName = ordered[0].VendorName;
            var description = ordered[0].Description;
            var classification = RecurringClassifier.Classify(vendorName, description, detected.Confidence);
            var normalized = RecurringClassifier.NormalizeMerchant(vendorName);

            _db.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                VendorName = vendorName,
                NormalizedMerchant = normalized,
                AverageAmount = detected.AverageAmount,
                Currency = ordered[0].Currency,
                Cadence = detected.Cadence,
                LastChargeDate = detected.LastDate,
                NextExpectedChargeDate = detected.NextDate,
                Status = SubscriptionStatus.Active,
                ConfidenceScore = detected.Confidence,
                ClassificationScore = classification.ClassificationScore,
                RecurringType = classification.Type,
                ClassificationReason = classification.Reason,
                ReviewStatus = ReviewStatus.None,
                UsageConfidenceScore = detected.Confidence,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeVendor(string name) =>
        string.Join(' ', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private bool TryDetect(IReadOnlyList<Domain.Entities.Transaction> ordered, DateOnly today,
        out DetectionResult detected)
    {
        detected = default!;

        var amounts = ordered.Select(t => t.Amount).ToList();
        if (!AmountsCompatible(amounts)) return false;

        var gaps = new List<int>();
        for (var i = 1; i < ordered.Count; i++)
            gaps.Add(ordered[i].TransactionDate.DayNumber - ordered[i - 1].TransactionDate.DayNumber);

        if (gaps.Count == 0) return false;

        var medianGap = gaps.OrderBy(g => g).ElementAt(gaps.Count / 2);
        var cadence = ClassifyCadence(medianGap);
        if (cadence == Cadence.Unknown) return false;

        var avgAmount = amounts.Average();
        var last = ordered[^1].TransactionDate;
        var next = cadence switch
        {
            Cadence.Weekly => last.AddDays(7),
            Cadence.Monthly => last.AddMonths(1),
            Cadence.Yearly => last.AddYears(1),
            _ => last
        };

        var confidence = Math.Min(100, 40 + ordered.Count * 15 + (gaps.All(g => SimilarGap(g, medianGap)) ? 20 : 0));

        detected = new DetectionResult(avgAmount, cadence, last, next, confidence);
        return true;
    }

    private static bool SimilarGap(int gap, int median) => Math.Abs(gap - median) <= Math.Max(3, median / 10);

    private static Cadence ClassifyCadence(int medianGapDays)
    {
        if (medianGapDays is >= 5 and <= 10) return Cadence.Weekly;
        if (medianGapDays is >= 25 and <= 35) return Cadence.Monthly;
        if (medianGapDays is >= 300 and <= 400) return Cadence.Yearly;
        return Cadence.Unknown;
    }

    private static bool AmountsCompatible(IReadOnlyList<decimal> amounts)
    {
        if (amounts.Count < 2) return false;
        var avg = amounts.Average();
        var max = amounts.Max();
        if (max == 0) return false;
        return amounts.All(a => Math.Abs(a - avg) / max <= AmountToleranceRatio);
    }

    private sealed record DetectionResult(decimal AverageAmount, Cadence Cadence, DateOnly LastDate,
        DateOnly NextDate, int Confidence);
}
