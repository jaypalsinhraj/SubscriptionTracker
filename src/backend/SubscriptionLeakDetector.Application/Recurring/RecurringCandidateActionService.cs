using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Recurring;

public sealed class RecurringCandidateActionService : IRecurringCandidateActionService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public RecurringCandidateActionService(IApplicationDbContext db, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
    }

    public async Task ClassifyAsync(Guid accountId, Guid actingUserId, Guid candidateId,
        ClassifyRecurringCandidateRequest request, CancellationToken cancellationToken = default)
    {
        var c = await _db.RecurringCandidates.FirstOrDefaultAsync(
            x => x.Id == candidateId && x.AccountId == accountId, cancellationToken);
        if (c == null) throw new InvalidOperationException("Recurring candidate not found.");

        var action = request.Action.Trim().ToLowerInvariant();
        var now = _clock.UtcNow;

        switch (action)
        {
            case "confirmsubscription":
            case "confirm_subscription":
            case "confirm":
            {
                var type = request.RecurringType ?? RecurringType.SoftwareSubscription;
                if (type != RecurringType.SoftwareSubscription && type != RecurringType.MediaSubscription)
                    type = RecurringType.SoftwareSubscription;

                var sub = new Subscription
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    VendorName = c.VendorName,
                    NormalizedMerchant = c.NormalizedMerchant,
                    AverageAmount = c.AverageAmount,
                    Currency = c.Currency,
                    Cadence = c.Cadence,
                    LastChargeDate = c.LastChargeDate,
                    NextExpectedChargeDate = c.NextExpectedChargeDate,
                    Status = SubscriptionStatus.Active,
                    ConfidenceScore = c.PatternConfidenceScore,
                    SubscriptionConfidenceScore = Math.Max(75, c.SubscriptionConfidenceScore),
                    IsSubscriptionCandidate = true,
                    RecurringType = type,
                    ClassificationReason = "User confirmed from review queue — " + c.ClassificationReason,
                    ReviewStatus = ReviewStatus.None,
                    UsageConfidenceScore = c.PatternConfidenceScore,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.Subscriptions.Add(sub);
                _db.RecurringCandidates.Remove(c);
                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(accountId, actingUserId, "recurring.candidate.confirmed", nameof(Subscription),
                    sub.Id.ToString(), $"Promoted candidate {candidateId}", cancellationToken);
                return;
            }
            case "dismiss":
            {
                _db.RecurringCandidates.Remove(c);
                await _db.SaveChangesAsync(cancellationToken);
                await _audit.LogAsync(accountId, actingUserId, "recurring.candidate.dismissed",
                    nameof(RecurringCandidate), candidateId.ToString(), "Dismissed", cancellationToken);
                return;
            }
            default:
                throw new InvalidOperationException("Unknown action. Use confirmSubscription or dismiss.");
        }
    }
}
