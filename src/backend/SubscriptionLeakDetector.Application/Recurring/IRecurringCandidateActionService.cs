namespace SubscriptionLeakDetector.Application.Recurring;

public interface IRecurringCandidateActionService
{
    Task ClassifyAsync(Guid accountId, Guid actingUserId, Guid candidateId, ClassifyRecurringCandidateRequest request,
        CancellationToken cancellationToken = default);
}
