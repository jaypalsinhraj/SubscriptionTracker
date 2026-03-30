using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionLeakDetector.Application.Diagnostics;
using SubscriptionLeakDetector.Application.Common.Interfaces;
using SubscriptionLeakDetector.Application.MerchantNormalization;
using SubscriptionLeakDetector.Domain.Entities;
using SubscriptionLeakDetector.Domain.Enums;

namespace SubscriptionLeakDetector.Application.Transactions;

public class TransactionImportService : ITransactionImportService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;
    private readonly IMerchantNormalizationService _merchant;
    private readonly ILogger<TransactionImportService> _logger;

    public TransactionImportService(IApplicationDbContext db, IDateTimeProvider clock, IAuditLogger audit,
        IMerchantNormalizationService merchant, ILogger<TransactionImportService> logger)
    {
        _db = db;
        _clock = clock;
        _audit = audit;
        _merchant = merchant;
        _logger = logger;
    }

    public async Task<Guid> ImportCsvAsync(Guid accountId, Guid? userId, ImportTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account == null) throw new InvalidOperationException("Account not found.");

        _logger.LogInformation(
            "Starting CSV import: AccountId={AccountId}, FileName={FileName}, ContentLength={Length}, DefaultCurrency={Currency}",
            accountId, request.FileName, request.CsvContent.Length, account.DefaultCurrency);

        IReadOnlyList<CsvTransactionRow> rows;
        try
        {
            rows = CsvTransactionParser.Parse(request.CsvContent, account.DefaultCurrency, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV parse failed for file {FileName}", request.FileName);
            throw;
        }

        #region agent log
        var debitRows = rows.Count(r => !r.IsCredit);
        var creditRows = rows.Count(r => r.IsCredit);
        DebugSessionNdjson.Write("H1", "TransactionImportService.ImportCsvAsync:afterParse",
            "Parsed CSV row mix",
            new { accountId, rowCount = rows.Count, debitRows, creditRows });
        DebugSessionNdjson.Write("H2", "TransactionImportService.ImportCsvAsync:afterParse",
            "Debit vs credit for recurring detection (debits required)",
            new { debitRows, creditRows, allCredits = debitRows == 0 });
        #endregion

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
            var raw = row.VendorName;
            var norm = _merchant.Normalize(raw, raw, null);
            _db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                TransactionImportId = import.Id,
                RawDescription = raw,
                VendorName = raw,
                Amount = row.Amount,
                IsCredit = row.IsCredit,
                Currency = row.Currency,
                TransactionDate = row.Date,
                Description = raw,
                NormalizedMerchant = norm.NormalizedKey,
                NormalizationConfidence = norm.Confidence,
                NormalizationReason = norm.Reason,
                MatchedNormalizationRule = norm.MatchedRuleName,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        #region agent log
        DebugSessionNdjson.Write("H1", "TransactionImportService.ImportCsvAsync:afterSave",
            "Transactions persisted after CSV import",
            new { accountId, importId = import.Id, persistedRows = rows.Count });
        #endregion

        _logger.LogInformation(
            "CSV import persisted: ImportId={ImportId}, TransactionRows={RowCount}, AccountId={AccountId}",
            import.Id, rows.Count, accountId);

        await _audit.LogAsync(accountId, userId, "transactions.import", nameof(TransactionImport),
            import.Id.ToString(), $"Imported {rows.Count} rows from {request.FileName}", cancellationToken);

        return import.Id;
    }
}
