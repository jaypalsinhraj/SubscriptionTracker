namespace SubscriptionLeakDetector.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public User? User { get; set; }
}
