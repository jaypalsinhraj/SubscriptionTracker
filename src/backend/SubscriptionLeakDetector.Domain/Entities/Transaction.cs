namespace SubscriptionLeakDetector.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? TransactionImportId { get; set; }
    public string VendorName { get; set; } = string.Empty;
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
