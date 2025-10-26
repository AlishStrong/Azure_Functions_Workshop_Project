namespace ImageProcessor.Services;

public interface IStorageService
{
    public Task<Stream> DownloadFile(string filePath);
    public Task DeleteFile(string filePath);
    public Task SetMetadataTags(Dictionary<string, string> metadataTags); 
}
