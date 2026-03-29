using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string VendorName { get; set; } = string.Empty;

    /// <summary>Lowercase, collapsed merchant key used for grouping and rules.</summary>
    public string NormalizedMerchant { get; set; } = string.Empty;

    public decimal AverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public Cadence Cadence { get; set; }
    public DateOnly LastChargeDate { get; set; }
    public DateOnly NextExpectedChargeDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    /// <summary>Recurring pattern strength from cadence/amount consistency (detection step).</summary>
    public int ConfidenceScore { get; set; }

    /// <summary>How likely this is a software/media subscription (classification step, 0–100).</summary>
    public int ClassificationScore { get; set; }

    public RecurringType RecurringType { get; set; }

    /// <summary>Short explanation of classification (keywords matched, etc.).</summary>
    public string ClassificationReason { get; set; } = string.Empty;

    /// <summary>Optional link to a user in the same account when ownership is internal.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Display owner (MVP — works without directory sync).</summary>
    public string? OwnerName { get; set; }

    public string? OwnerEmail { get; set; }
    public ReviewStatus ReviewStatus { get; set; }
    public DateTimeOffset? LastConfirmedInUseAt { get; set; }
    public DateTimeOffset? LastReviewRequestedAt { get; set; }
    public DateOnly? NextReviewDate { get; set; }

    /// <summary>Optional; mirrors detection confidence for review prioritisation.</summary>
    public int? UsageConfidenceScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<RenewalAlert> Alerts { get; set; } = new List<RenewalAlert>();
}
