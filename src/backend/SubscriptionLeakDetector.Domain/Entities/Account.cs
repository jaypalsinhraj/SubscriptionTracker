namespace SubscriptionLeakDetector.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>BCP 47 locale for UI formatting (e.g. en-GB, en-US).</summary>
    public string UiCulture { get; set; } = "en-US";
    /// <summary>ISO 4217 code for display and CSV defaults (e.g. USD, GBP).</summary>
    public string DefaultCurrency { get; set; } = "USD";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<BankConnection> BankConnections { get; set; } = new List<BankConnection>();
    public ICollection<TransactionImport> TransactionImports { get; set; } = new List<TransactionImport>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<RenewalAlert> RenewalAlerts { get; set; } = new List<RenewalAlert>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
