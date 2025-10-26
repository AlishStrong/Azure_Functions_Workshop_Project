using System;
using Microsoft.Extensions.Logging;

namespace ImageProcessor.Services;

public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;

    public StorageService(ILogger<StorageService> logger)
    {
        _logger = logger;
    }

    public Task<Stream> DownloadFile(string filePath)
    {
        throw new NotImplementedException();
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
