using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using SubscriptionLeakDetector.Application.Common.Interfaces;

namespace SubscriptionLeakDetector.Infrastructure.Storage;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureStorage";
    public string ConnectionString { get; set; } = string.Empty;
    public string ImportsContainer { get; set; } = "imports";
}

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadImportAsync(Guid accountId, string fileName, Stream content,
        CancellationToken cancellationToken = default)
    {
        var client = new BlobServiceClient(_options.ConnectionString);
        var container = client.GetBlobContainerClient(_options.ImportsContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobName = $"{accountId}/{Guid.NewGuid():N}_{fileName}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }
}
