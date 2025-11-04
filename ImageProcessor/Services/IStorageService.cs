namespace ImageProcessor.Services;

public interface IStorageService
{
    public Task<Stream> DownloadFile(string filePath);
    public Task DeleteFile(string filePath);
    public Task SetMetadataTags(string filePath, Dictionary<string, string> metadataTags); 
}
