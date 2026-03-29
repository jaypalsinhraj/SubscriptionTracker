using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Alerts;

public record AlertListItemDto(
    Guid Id,
    Guid? SubscriptionId,
    AlertType AlertType,
    AlertSeverity Severity,
    string Title,
    string Message,
    bool IsRead,
    AlertStatus AlertStatus,
    AlertResponseType? ResponseType,
    DateTimeOffset? RespondedAt,
    Guid? RespondedByUserId,
    string? Notes,
    DateTimeOffset CreatedAt);
