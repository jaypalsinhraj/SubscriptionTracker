namespace SubscriptionLeakDetector.Application.Transactions;

public interface ITransactionImportService
{
    Task<Guid> ImportCsvAsync(Guid accountId, Guid? userId, ImportTransactionRequest request,
        CancellationToken cancellationToken = default);
}
