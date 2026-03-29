using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Infrastructure.Storage;

/// <summary>
/// Placeholder when Azure Blob is not configured; returns a virtual path for auditability.
/// </summary>
public sealed class NoOpBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImportAsync(Guid accountId, string fileName, Stream content,
        CancellationToken cancellationToken = default)
    {
        _ = content;
        return Task.FromResult($"noop://{accountId}/{fileName}");
    }
}
