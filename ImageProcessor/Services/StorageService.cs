using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
        using MemoryStream memoryStream = new();

        if (blobClient.Exists())
        {
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;
            using StreamReader streamReader = new(memoryStream);
            string blobContent = await streamReader.ReadToEndAsync();
            _logger.LogWarning($"Content of {filePath} is: '{blobContent}'");
        } else
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
