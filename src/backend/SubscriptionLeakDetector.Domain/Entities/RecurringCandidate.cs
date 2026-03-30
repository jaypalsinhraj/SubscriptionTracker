using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Domain.Entities;

/// <summary>
/// A detected recurring pattern that is not promoted to a <see cref="Subscription"/> (review bucket or non-subscription recurring).
/// </summary>
public class RecurringCandidate
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }

    /// <summary>Stable deduplication key within the account (normalized merchant key + cadence).</summary>
    public string GroupKey { get; set; } = string.Empty;

    public string VendorName { get; set; } = string.Empty;

    /// <summary>Lowercase canonical merchant key for matching.</summary>
    public string NormalizedMerchant { get; set; } = string.Empty;

    public RecurringType RecurringType { get; set; }
    public int SubscriptionConfidenceScore { get; set; }
    public string ClassificationReason { get; set; } = string.Empty;
    public int PatternConfidenceScore { get; set; }
    public Cadence Cadence { get; set; }
    public decimal AverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateOnly LastChargeDate { get; set; }
    public DateOnly NextExpectedChargeDate { get; set; }
    public RecurringCandidateStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
