namespace SubscriptionLeakDetector.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadImportAsync(Guid accountId, string fileName, Stream content,
        CancellationToken cancellationToken = default);
}
