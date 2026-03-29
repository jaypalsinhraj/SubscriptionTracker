namespace SubscriptionLeakDetector.Application.Transactions;

public record ImportTransactionRequest(string FileName, string CsvContent);
