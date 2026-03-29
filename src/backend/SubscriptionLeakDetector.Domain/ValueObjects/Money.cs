namespace SubscriptionLeakDetector.Domain.ValueObjects;

/// <summary>
/// Simple money representation for domain clarity; persisted as decimal + currency on entities.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Usd(decimal amount) => new(amount, "USD");
}
