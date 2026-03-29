using Microsoft.EntityFrameworkCore;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Transactions;

public class TransactionImportService : ITransactionImportService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;

    public TransactionImportService(IApplicationDbContext db, IDateTimeProvider clock, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
    }

    public async Task<Guid> ImportCsvAsync(Guid accountId, Guid? userId, ImportTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account == null) throw new InvalidOperationException("Account not found.");

        var rows = CsvTransactionParser.Parse(request.CsvContent, account.DefaultCurrency);
        var now = _clock.UtcNow;

        var import = new TransactionImport
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            FileName = request.FileName,
            Status = ImportStatus.Completed,
            RowCount = rows.Count,
            CreatedAt = now,
            CompletedAt = now
        };

        _db.TransactionImports.Add(import);

        foreach (var row in rows)
        {
            _db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                TransactionImportId = import.Id,
                VendorName = row.VendorName,
                Amount = row.Amount,
                IsCredit = row.IsCredit,
                Currency = row.Currency,
                TransactionDate = row.Date,
                Description = null,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(accountId, userId, "transactions.import", nameof(TransactionImport),
            import.Id.ToString(), $"Imported {rows.Count} rows from {request.FileName}", cancellationToken);

        return import.Id;
    }
}
