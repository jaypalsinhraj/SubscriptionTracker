using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Domain.Entities;

public class TransactionImport
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public ImportStatus Status { get; set; }
    public int RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
