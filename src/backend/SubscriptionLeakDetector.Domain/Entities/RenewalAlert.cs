using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Domain.Entities;

public class RenewalAlert
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public AlertStatus AlertStatus { get; set; }
    public AlertResponseType? ResponseType { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public Guid? RespondedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Subscription? Subscription { get; set; }
}
