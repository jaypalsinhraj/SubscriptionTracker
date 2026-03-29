namespace SubscriptionLeakDetector.Api.Options;

/// <summary>
/// Fallback globalization when an account has no explicit culture/currency set.
/// </summary>
public sealed class GlobalizationOptions
{
    public const string SectionName = "Globalization";

    /// <summary>BCP 47 (e.g. en-GB).</summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>ISO 4217 (e.g. GBP).</summary>
    public string DefaultCurrency { get; set; } = "USD";
}
