namespace SubscriptionLeakDetector.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? AccountId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
