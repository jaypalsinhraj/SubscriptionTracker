namespace SubscriptionLeakDetector.Domain.Enums;

public enum AlertStatus
{
    Open = 0,
    PendingConfirmation = 1,
    Resolved = 2,
    Dismissed = 3
}
