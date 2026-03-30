namespace SubscriptionLeakDetector.Application.Accounts;

public sealed class ResetAccountDataRequest
{
    /// <summary>Must be true to perform the reset.</summary>
    public bool Confirm { get; set; }
}
