using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Subscriptions;

public sealed class SubscriptionReviewService : ISubscriptionReviewService
{
    private const int ConfirmationCooldownDays = 90;
    private const int NotSureFollowUpDays = 14;

    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IDateTimeProvider _clock;

    public SubscriptionReviewService(IApplicationDbContext db, IAuditLogger audit, IDateTimeProvider clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async Task<SubscriptionListItemDto?> AssignOwnerAsync(Guid accountId, Guid actingUserId,
        Guid subscriptionId, AssignOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(
            s => s.Id == subscriptionId && s.AccountId == accountId, cancellationToken);
        if (sub == null) return null;

        if (request.OwnerUserId.HasValue)
        {
            var ownerExists = await _db.Users.AsNoTracking().AnyAsync(
                u => u.Id == request.OwnerUserId.Value && u.AccountId == accountId, cancellationToken);
            if (!ownerExists)
                throw new InvalidOperationException("OwnerUserId must reference a user in this account.");
        }

        sub.OwnerUserId = request.OwnerUserId;
        sub.OwnerName = string.IsNullOrWhiteSpace(request.OwnerName) ? null : request.OwnerName.Trim();
        sub.OwnerEmail = string.IsNullOrWhiteSpace(request.OwnerEmail) ? null : request.OwnerEmail.Trim();
        sub.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(accountId, actingUserId, "subscription.owner.assigned", nameof(Subscription),
            sub.Id.ToString(),
            $"Owner updated: name={sub.OwnerName}, email={sub.OwnerEmail}, userId={sub.OwnerUserId}", cancellationToken);

        return await GetSubscriptionDtoAsync(subscriptionId, cancellationToken);
    }

    public async Task RequestReviewAsync(Guid accountId, Guid actingUserId, Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(
            s => s.Id == subscriptionId && s.AccountId == accountId, cancellationToken);
        if (sub == null) throw new InvalidOperationException("Subscription not found.");

        var now = _clock.UtcNow;
        var today = _clock.TodayUtc;

        sub.ReviewStatus = ReviewStatus.NeedsReview;
        sub.LastReviewRequestedAt = now;
        sub.UpdatedAt = now;

        var already = await _db.RenewalAlerts.AnyAsync(
            a => a.AccountId == accountId && a.SubscriptionId == subscriptionId &&
                 a.AlertType == AlertType.OwnerConfirmationRequest &&
                 (a.AlertStatus == AlertStatus.Open || a.AlertStatus == AlertStatus.PendingConfirmation),
            cancellationToken);

        if (!already)
        {
            _db.RenewalAlerts.Add(new RenewalAlert
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                SubscriptionId = subscriptionId,
                AlertType = AlertType.OwnerConfirmationRequest,
                Severity = AlertSeverity.Warning,
                Title = $"Confirmation requested: {sub.VendorName}",
                Message = "Please confirm whether this subscription is still needed.",
                IsRead = false,
                AlertStatus = AlertStatus.PendingConfirmation,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(accountId, actingUserId, "subscription.review.requested", nameof(Subscription),
            sub.Id.ToString(), "Manual review requested", cancellationToken);
    }

    public async Task<AlertRespondResultDto> RespondToAlertAsync(Guid accountId, Guid actingUserId, Guid alertId,
        RespondToAlertRequest request, CancellationToken cancellationToken = default)
    {
        var alert = await _db.RenewalAlerts
            .Include(a => a.Subscription)
            .FirstOrDefaultAsync(a => a.Id == alertId && a.AccountId == accountId, cancellationToken);
        if (alert == null) throw new InvalidOperationException("Alert not found.");

        if (alert.AlertStatus == AlertStatus.Resolved || alert.AlertStatus == AlertStatus.Dismissed)
            throw new InvalidOperationException("This alert is already closed.");

        if (!IsConfirmationWorkflow(alert.AlertType))
            throw new InvalidOperationException("This alert does not accept confirmation responses.");

        if (alert.AlertType == AlertType.DuplicateTool)
            throw new InvalidOperationException("Duplicate-tool alerts cannot be confirmed here.");

        var sub = alert.Subscription;
        if (sub == null && alert.SubscriptionId.HasValue)
            sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == alert.SubscriptionId.Value, cancellationToken);
        if (sub == null) throw new InvalidOperationException("Subscription not found for this alert.");

        var now = _clock.UtcNow;
        var today = _clock.TodayUtc;

        alert.ResponseType = request.Response;
        alert.RespondedAt = now;
        alert.RespondedByUserId = actingUserId;
        alert.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        alert.IsRead = true;

        switch (request.Response)
        {
            case AlertResponseType.StillNeeded:
                alert.AlertStatus = AlertStatus.Resolved;
                sub.ReviewStatus = ReviewStatus.ConfirmedNeeded;
                sub.LastConfirmedInUseAt = now;
                sub.NextReviewDate = today.AddDays(ConfirmationCooldownDays);
                sub.UpdatedAt = now;
                await _audit.LogAsync(accountId, actingUserId, "subscription.review.confirmed_needed",
                    nameof(Subscription), sub.Id.ToString(), "Still needed", cancellationToken);
                break;

            case AlertResponseType.NotNeeded:
                alert.AlertStatus = AlertStatus.Resolved;
                sub.ReviewStatus = ReviewStatus.MarkedForCancellation;
                sub.UpdatedAt = now;
                await _audit.LogAsync(accountId, actingUserId, "subscription.review.marked_cancellation",
                    nameof(Subscription), sub.Id.ToString(), "Not needed", cancellationToken);
                break;

            case AlertResponseType.NotSure:
                alert.AlertStatus = AlertStatus.PendingConfirmation;
                sub.ReviewStatus = ReviewStatus.UnderReview;
                sub.NextReviewDate = today.AddDays(NotSureFollowUpDays);
                sub.UpdatedAt = now;
                await _audit.LogAsync(accountId, actingUserId, "subscription.review.under_review",
                    nameof(Subscription), sub.Id.ToString(), "Not sure — follow-up scheduled", cancellationToken);
                break;
        }

        await _audit.LogAsync(accountId, actingUserId, "alert.review.responded", nameof(RenewalAlert), alert.Id.ToString(),
            $"Response={request.Response}", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new AlertRespondResultDto(alert.Id, alert.AlertStatus, sub.Id, sub.ReviewStatus, sub.NextReviewDate);
    }

    private static bool IsConfirmationWorkflow(AlertType type) =>
        type is AlertType.RenewalApproaching or AlertType.SuspectedUnused or AlertType.OwnerConfirmationRequest
            or AlertType.OwnerMissing;

    private async Task<SubscriptionListItemDto?> GetSubscriptionDtoAsync(Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        return await _db.Subscriptions.AsNoTracking()
            .Where(s => s.Id == subscriptionId)
            .Select(s => new SubscriptionListItemDto(
                s.Id,
                s.VendorName,
                s.NormalizedMerchant,
                s.RecurringType,
                s.ClassificationScore,
                s.ClassificationReason,
                s.AverageAmount,
                s.Currency,
                s.Cadence,
                s.LastChargeDate,
                s.NextExpectedChargeDate,
                s.Status,
                s.ConfidenceScore,
                s.OwnerUserId,
                s.OwnerName,
                s.OwnerEmail,
                s.ReviewStatus,
                s.LastConfirmedInUseAt,
                s.LastReviewRequestedAt,
                s.NextReviewDate,
                s.UsageConfidenceScore))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
