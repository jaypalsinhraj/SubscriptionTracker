using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public record AssignOwnerRequest(string? OwnerName, string? OwnerEmail, Guid? OwnerUserId);

public record RespondToAlertRequest(AlertResponseType Response, string? Notes);

public record AlertRespondResultDto(
    Guid AlertId,
    AlertStatus AlertStatus,
    Guid? SubscriptionId,
    ReviewStatus SubscriptionReviewStatus,
    DateOnly? NextReviewDate);
