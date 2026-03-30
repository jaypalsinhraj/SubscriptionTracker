namespace SubscriptionLeakDetector.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? TransactionImportId { get; set; }

    /// <summary>Original bank description line (may match <see cref="VendorName"/> when CSV has a single text column).</summary>
    public string? RawDescription { get; set; }

    public string VendorName { get; set; } = string.Empty;

    /// <summary>Lowercase canonical key from merchant normalization, used for recurring grouping.</summary>
    public string? NormalizedMerchant { get; set; }

    /// <summary>0–100 confidence for the normalization / alias match.</summary>
    public int NormalizationConfidence { get; set; }

    public string? NormalizationReason { get; set; }

    /// <summary>Which alias or rule produced the normalized merchant, if any.</summary>
    public string? MatchedNormalizationRule { get; set; }
    /// <summary>Absolute transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// True when the row represents money in (e.g. UK bank export: positive amount). Excluded from subscription pattern detection.
    /// </summary>
    public bool IsCredit { get; set; }
    public string Currency { get; set; } = "USD";
    public DateOnly TransactionDate { get; set; }
    public string? Description { get; set; }
    public string? RawCategory { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public TransactionImport? TransactionImport { get; set; }
}
