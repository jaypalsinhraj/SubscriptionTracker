using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Application.Alerts;

public class AlertQueryService : IAlertQueryService
{
    private readonly IApplicationDbContext _db;

    public AlertQueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AlertListItemDto>> ListAsync(Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _db.RenewalAlerts
            .AsNoTracking()
            .Where(a => a.AccountId == accountId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertListItemDto(
                a.Id,
                a.SubscriptionId,
                a.AlertType,
                a.Severity,
                a.Title,
                a.Message,
                a.IsRead,
                a.AlertStatus,
                a.ResponseType,
                a.RespondedAt,
                a.RespondedByUserId,
                a.Notes,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
