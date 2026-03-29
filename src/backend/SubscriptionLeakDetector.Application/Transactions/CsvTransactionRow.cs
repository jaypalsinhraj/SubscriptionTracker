namespace SubscriptionLeakDetector.Application.Transactions;

/// <param name="IsCredit">True when the raw statement amount indicates money in (default UK: positive).</param>
public sealed record CsvTransactionRow(DateOnly Date, decimal Amount, bool IsCredit, string VendorName, string Currency);
