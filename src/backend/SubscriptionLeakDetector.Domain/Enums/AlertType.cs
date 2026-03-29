namespace SubscriptionLeakDetector.Domain.Enums;

public enum AlertType
{
    RenewalApproaching = 0,
    DuplicateTool = 1,
    SuspectedUnused = 2,
    /// <summary>Explicit confirmation request (e.g. manual "request review").</summary>
    OwnerConfirmationRequest = 3,
    /// <summary>No owner assigned when confirmation is needed.</summary>
    OwnerMissing = 4
}
