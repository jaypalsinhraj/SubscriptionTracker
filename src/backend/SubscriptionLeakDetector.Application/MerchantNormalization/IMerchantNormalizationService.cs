namespace SubscriptionLeakDetector.Application.MerchantNormalization;

public interface IMerchantNormalizationService
{
    /// <summary>
    /// Derives a canonical merchant key and display label from messy bank text.
    /// </summary>
    MerchantNormalizationResult Normalize(string? rawDescription, string? vendorName, string? rawCategory);
}
