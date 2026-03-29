namespace SubscriptionLeakDetector.Domain.Enums;

public enum ReviewStatus
{
    None = 0,
    NeedsReview = 1,
    UnderReview = 2,
    ConfirmedNeeded = 3,
    MarkedForCancellation = 4,
    CancellationPlanned = 5
}
