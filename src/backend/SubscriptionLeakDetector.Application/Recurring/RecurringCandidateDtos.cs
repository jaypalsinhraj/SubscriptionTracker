using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Recurring;

public record RecurringCandidateListItemDto(
    Guid Id,
    string VendorName,
    string NormalizedMerchant,
    RecurringType RecurringType,
    int SubscriptionConfidenceScore,
    string ClassificationReason,
    int PatternConfidenceScore,
    Cadence Cadence,
    decimal AverageAmount,
    string Currency,
    DateOnly LastChargeDate,
    DateOnly NextExpectedChargeDate,
    RecurringCandidateStatus Status,
    string UiLabel);

public sealed class ClassifyRecurringCandidateRequest
{
    /// <summary>confirmSubscription | dismiss</summary>
    public string Action { get; set; } = "";

    /// <summary>When confirming, optional override (defaults to SoftwareSubscription).</summary>
    public RecurringType? RecurringType { get; set; }
}
