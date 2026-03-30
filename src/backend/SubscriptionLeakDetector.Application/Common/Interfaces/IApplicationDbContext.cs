using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Domain.Entities;

namespace SubscriptionLeakDetector.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<User> Users { get; }
    DbSet<BankConnection> BankConnections { get; }
    DbSet<TransactionImport> TransactionImports { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<RecurringCandidate> RecurringCandidates { get; }
    DbSet<RenewalAlert> RenewalAlerts { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
