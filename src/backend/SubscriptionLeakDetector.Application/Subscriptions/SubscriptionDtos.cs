using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public record SubscriptionListItemDto(
    Guid Id,
    string VendorName,
    string NormalizedMerchant,
    RecurringType RecurringType,
    int SubscriptionConfidenceScore,
    string ClassificationReason,
    bool IsSubscriptionCandidate,
    decimal AverageAmount,
    string Currency,
    Cadence Cadence,
    DateOnly LastChargeDate,
    DateOnly NextExpectedChargeDate,
    SubscriptionStatus Status,
    int PatternConfidenceScore,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerEmail,
    ReviewStatus ReviewStatus,
    DateTimeOffset? LastConfirmedInUseAt,
    DateTimeOffset? LastReviewRequestedAt,
    DateOnly? NextReviewDate,
    int? UsageConfidenceScore);
