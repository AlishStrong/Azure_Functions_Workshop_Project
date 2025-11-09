using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace ImageProcessor.Services;

public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;
    private readonly BlobContainerClient _imagesContainerClient;

    public StorageService(
        ILogger<StorageService> logger,
        BlobContainerClient imagesContainerClient
    )
    {
        _logger = logger;
        _imagesContainerClient = imagesContainerClient;
    }

    public async Task<Stream> DownloadFile(string filePath)
    {
        BlobClient blobClient = _imagesContainerClient.GetBlobClient(filePath);
        MemoryStream memoryStream = new();

        if (await blobClient.ExistsAsync())
        {
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;
        }
        else
        {
            _logger.LogWarning($"Blob {filePath} does not exist in Storage Account '{blobClient.AccountName}'.");
        }
        
        return memoryStream;
    }

    public async Task DeleteFile(string filePath)
    {
        BlobClient blobClient = _imagesContainerClient.GetBlobClient(filePath);

        if (blobClient.Exists() && (await blobClient.DeleteIfExistsAsync()).Value)
        {
            _logger.LogWarning($"Blob {filePath} was deleted.");
        }
        else
        {
            _logger.LogWarning($"Blob {filePath} does not exist in Storage Account '{blobClient.AccountName}'.");
        }
    }

    public async Task SetMetadataTags(string filePath, Dictionary<string, string> metadataTags)
    {
        BlobClient blobClient = _imagesContainerClient.GetBlobClient(filePath);

        if (blobClient.Exists())
        {
            await blobClient.SetMetadataAsync(metadataTags);
        } else
        {
            _logger.LogWarning($"Cannot set Metadata Tags on Blob {filePath}. It does not exist in Storage Account '{blobClient.AccountName}'.");
        }
    }
}
