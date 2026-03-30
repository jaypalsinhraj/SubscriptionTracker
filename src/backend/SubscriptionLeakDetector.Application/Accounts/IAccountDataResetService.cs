namespace SubscriptionLeakDetector.Application.Accounts;

public interface IAccountDataResetService
{
    /// <summary>
    /// Removes all imported and derived data for the account. Keeps the account row and users.
    /// </summary>
    Task ResetAllDataAsync(Guid accountId, CancellationToken cancellationToken = default);
}
