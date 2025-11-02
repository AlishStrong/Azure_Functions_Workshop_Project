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

        if (blobClient.Exists())
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

    public Task DeleteFile(string filePath)
    {
        throw new NotImplementedException();
    }

    public Task SetMetadataTags(Dictionary<string, string> metadataTags)
    {
        throw new NotImplementedException();
    }
}
