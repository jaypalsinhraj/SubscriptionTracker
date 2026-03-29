using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Classification;

public readonly record struct RecurringClassificationResult(
    RecurringType Type,
    int ClassificationScore,
    string Reason);
