using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionLeakDetector.Application.Classification;
using SubscriptionLeakDetector.Application.Diagnostics;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.MerchantNormalization;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public class RecurringDetectionService : IRecurringDetectionService
{
    private const decimal AmountToleranceRatio = 0.08m;

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IMerchantNormalizationService _merchant;
    private readonly ILogger<RecurringDetectionService> _logger;

    public RecurringDetectionService(IApplicationDbContext db, IDateTimeProvider clock,
        IMerchantNormalizationService merchant, ILogger<RecurringDetectionService> logger)
    {
        _db = db;
        _clock = clock;
        _merchant = merchant;
        _logger = logger;
    }

    public async Task RunForAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await NormalizeTransactionsForAccountAsync(accountId, cancellationToken);

        var txs = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId && !t.IsCredit)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        if (txs.Count == 0)
            _logger.LogWarning(
                "Recurring detection: no debit transactions for AccountId={AccountId} (credits are ignored).",
                accountId);

        var groups = txs
            .GroupBy(t => GroupingKey(t))
            .Where(g => g.Count() >= 2)
            .ToList();

        if (txs.Count > 0 && groups.Count == 0)
            _logger.LogWarning(
                "Recurring detection: no recurring groups for AccountId={AccountId} (need 2+ debits with similar amounts per merchant). DebitTxCount={DebitCount}",
                accountId, txs.Count);

        #region agent log
        DebugSessionNdjson.Write("H3", "RecurringDetectionService.RunForAccountAsync:afterGroup",
            "Debit transactions and recurring-eligible groups",
            new { accountId, debitTxCount = txs.Count, groupCount = groups.Count });
        #endregion

        var existingSubs = await _db.Subscriptions.Where(s => s.AccountId == accountId).ToListAsync(cancellationToken);
        _db.Subscriptions.RemoveRange(existingSubs);

        var existingCandidates =
            await _db.RecurringCandidates.Where(c => c.AccountId == accountId).ToListAsync(cancellationToken);
        _db.RecurringCandidates.RemoveRange(existingCandidates);

        var now = _clock.UtcNow;
        var today = _clock.TodayUtc;
        var subscriptionCount = 0;
        var candidateCount = 0;
        var tryDetectFailCount = 0;
        var droppedNoBranchCount = 0;
        var droppedSamples = new List<(string Type, int Score)>();

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(t => t.TransactionDate).ToList();
            if (!TryDetect(ordered, today, out var detected))
            {
                tryDetectFailCount++;
                continue;
            }

            var vendorName = ordered[0].VendorName;
            var rawDesc = ordered[0].RawDescription ?? ordered[0].VendorName;
            var norm = _merchant.Normalize(rawDesc, vendorName, ordered[0].RawCategory);
            var classification = RecurringClassifier.Classify(vendorName, rawDesc, detected.Confidence, norm);

            var stableKey = $"{norm.NormalizedKey}|{(int)detected.Cadence}";

            if (RecurringClassifier.IsLikelySubscriptionRow(classification.Type,
                    classification.SubscriptionConfidenceScore))
            {
                subscriptionCount++;
                _db.Subscriptions.Add(new Subscription
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    VendorName = vendorName,
                    NormalizedMerchant = norm.NormalizedKey,
                    AverageAmount = detected.AverageAmount,
                    Currency = ordered[0].Currency,
                    Cadence = detected.Cadence,
                    LastChargeDate = detected.LastDate,
                    NextExpectedChargeDate = detected.NextDate,
                    Status = SubscriptionStatus.Active,
                    ConfidenceScore = detected.Confidence,
                    SubscriptionConfidenceScore = classification.SubscriptionConfidenceScore,
                    IsSubscriptionCandidate = true,
                    RecurringType = classification.Type,
                    ClassificationReason = classification.Reason,
                    ReviewStatus = ReviewStatus.None,
                    UsageConfidenceScore = detected.Confidence,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else if (RecurringClassifier.IsReviewBucketRow(classification.Type,
                         classification.SubscriptionConfidenceScore))
            {
                candidateCount++;
                AddCandidate(accountId, stableKey, vendorName, norm.NormalizedKey, classification, detected, now,
                    RecurringCandidateStatus.NeedsReview);
            }
            else if (RecurringClassifier.IsNonSubscriptionRecurringType(classification.Type))
            {
                candidateCount++;
                AddCandidate(accountId, stableKey, vendorName, norm.NormalizedKey, classification, detected, now,
                    RecurringCandidateStatus.NonSubscriptionRecurring);
            }
            else
            {
                droppedNoBranchCount++;
                if (droppedSamples.Count < 4)
                    droppedSamples.Add((classification.Type.ToString(), classification.SubscriptionConfidenceScore));
            }
        }

        #region agent log
        DebugSessionNdjson.Write("H3", "RecurringDetectionService.RunForAccountAsync:loopSummary",
            "Pattern detection and classification routing",
            new
            {
                accountId,
                tryDetectFailCount,
                tryDetectOkCount = groups.Count - tryDetectFailCount,
                subscriptionCount,
                candidateCount,
                droppedNoBranchCount,
                droppedSamples = droppedSamples.Select(s => $"{s.Type}:{s.Score}").ToList()
            });
        DebugSessionNdjson.Write("H4", "RecurringDetectionService.RunForAccountAsync:loopSummary",
            "Subscription table only fills on SoftwareSubscription/MediaSubscription with score>=70",
            new
            {
                subscriptionRowsWritten = subscriptionCount,
                droppedUnclassified = droppedNoBranchCount,
                recurringCandidatesWritten = candidateCount
            });
        #endregion

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recurring detection finished for AccountId={AccountId}: debitTx={DebitCount}, eligibleGroups={GroupCount}, subscriptionsCreated={SubscriptionCount}, recurringCandidates={CandidateCount}",
            accountId, txs.Count, groups.Count, subscriptionCount, candidateCount);
    }

    private void AddCandidate(Guid accountId, string groupKey, string vendorName, string normalizedKey,
        RecurringClassificationResult classification, DetectionResult detected, DateTimeOffset now,
        RecurringCandidateStatus status)
    {
        _db.RecurringCandidates.Add(new RecurringCandidate
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            GroupKey = groupKey,
            VendorName = vendorName,
            NormalizedMerchant = normalizedKey,
            RecurringType = classification.Type,
            SubscriptionConfidenceScore = classification.SubscriptionConfidenceScore,
            ClassificationReason = classification.Reason,
            PatternConfidenceScore = detected.Confidence,
            Cadence = detected.Cadence,
            AverageAmount = detected.AverageAmount,
            Currency = detected.Currency,
            LastChargeDate = detected.LastDate,
            NextExpectedChargeDate = detected.NextDate,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private async Task NormalizeTransactionsForAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var txs = await _db.Transactions
            .Where(t => t.AccountId == accountId)
            .ToListAsync(cancellationToken);

        foreach (var t in txs)
        {
            var raw = t.RawDescription ?? t.VendorName;
            var norm = _merchant.Normalize(raw, t.VendorName, t.RawCategory);
            t.NormalizedMerchant = norm.NormalizedKey;
            t.NormalizationConfidence = norm.Confidence;
            t.NormalizationReason = norm.Reason;
            t.MatchedNormalizationRule = norm.MatchedRuleName;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Recurring groups must share a stable cadence and similar amounts. A single merchant alias (e.g.
    /// <c>adobe</c>) can match several product lines at different prices — grouping only by
    /// <see cref="Transaction.NormalizedMerchant"/> merges them and makes <see cref="AmountsCompatible"/> fail.
    /// Appending the normalized raw vendor line splits those into one group per bank description.
    /// </summary>
    private static string GroupingKey(Transaction t)
    {
        var baseKey = !string.IsNullOrWhiteSpace(t.NormalizedMerchant) && t.NormalizationConfidence > 0
            ? t.NormalizedMerchant!
            : NormalizeVendor($"{t.VendorName} {t.Description ?? ""}");

        var vendorLine = NormalizeVendor(t.VendorName ?? "");
        return $"{baseKey}|{vendorLine}";
    }

    private static string NormalizeVendor(string name) =>
        string.Join(' ', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private bool TryDetect(IReadOnlyList<Transaction> ordered, DateOnly today, out DetectionResult detected)
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
            Cadence.Quarterly => last.AddMonths(3),
            Cadence.Yearly => last.AddYears(1),
            _ => last
        };

        var confidence = Math.Min(100, 40 + ordered.Count * 15 + (gaps.All(g => SimilarGap(g, medianGap)) ? 20 : 0));

        detected = new DetectionResult(avgAmount, ordered[0].Currency, cadence, last, next, confidence);
        return true;
    }

    private static bool SimilarGap(int gap, int median) => Math.Abs(gap - median) <= Math.Max(3, median / 10);

    private static Cadence ClassifyCadence(int medianGapDays)
    {
        if (medianGapDays is >= 5 and <= 10) return Cadence.Weekly;
        if (medianGapDays is >= 25 and <= 35) return Cadence.Monthly;
        if (medianGapDays is >= 80 and <= 105) return Cadence.Quarterly;
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

    private sealed record DetectionResult(decimal AverageAmount, string Currency, Cadence Cadence, DateOnly LastDate,
        DateOnly NextDate, int Confidence);
}
