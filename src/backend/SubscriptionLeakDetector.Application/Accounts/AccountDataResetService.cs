using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Application.Accounts;

public sealed class AccountDataResetService : IAccountDataResetService
{
    private readonly IApplicationDbContext _db;

    public AccountDataResetService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task ResetAllDataAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var alerts = await _db.RenewalAlerts.Where(a => a.AccountId == accountId).ToListAsync(cancellationToken);
        _db.RenewalAlerts.RemoveRange(alerts);

        var subscriptions = await _db.Subscriptions.Where(s => s.AccountId == accountId).ToListAsync(cancellationToken);
        _db.Subscriptions.RemoveRange(subscriptions);

        var recurring = await _db.RecurringCandidates.Where(c => c.AccountId == accountId).ToListAsync(cancellationToken);
        _db.RecurringCandidates.RemoveRange(recurring);

        var transactions = await _db.Transactions.Where(t => t.AccountId == accountId).ToListAsync(cancellationToken);
        _db.Transactions.RemoveRange(transactions);

        var imports = await _db.TransactionImports.Where(i => i.AccountId == accountId).ToListAsync(cancellationToken);
        _db.TransactionImports.RemoveRange(imports);

        var auditLogs = await _db.AuditLogs.Where(a => a.AccountId == accountId).ToListAsync(cancellationToken);
        _db.AuditLogs.RemoveRange(auditLogs);

        var banks = await _db.BankConnections.Where(b => b.AccountId == accountId).ToListAsync(cancellationToken);
        _db.BankConnections.RemoveRange(banks);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
