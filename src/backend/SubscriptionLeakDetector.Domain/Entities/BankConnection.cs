using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Domain.Entities;

public class BankConnection
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BankConnectionStatus Status { get; set; }
    public string? InstitutionName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
