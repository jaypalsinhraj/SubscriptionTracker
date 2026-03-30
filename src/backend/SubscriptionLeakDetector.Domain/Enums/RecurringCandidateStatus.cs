namespace SubscriptionLeakDetector.Domain.Enums;

/// <summary>
/// Persisted recurring pattern that is not a confirmed subscription row.
/// </summary>
public enum RecurringCandidateStatus
{
    /// <summary>Borderline subscription likelihood — user may confirm or reclassify.</summary>
    NeedsReview = 0,

    /// <summary>Recurring charge classified as non-subscription (utilities, rent, payroll, etc.).</summary>
    NonSubscriptionRecurring = 1
}
