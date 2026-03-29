namespace SubscriptionLeakDetector.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(Guid accountId, Guid? userId, string action, string entityType, string? entityId, string details,
        CancellationToken cancellationToken = default);
}
