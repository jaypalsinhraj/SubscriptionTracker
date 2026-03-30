using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.MerchantNormalization;

public readonly record struct MerchantNormalizationResult(
    string NormalizedKey,
    string DisplayName,
    int Confidence,
    string Reason,
    string? MatchedRuleName,
    RecurringType? HintedRecurringType);
