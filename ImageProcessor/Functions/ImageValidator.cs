using System.Net;
using System.Text.Json;
using ImageProcessor.Common;
using ImageProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ImageProcessor;

public class ImageValidator
{
    //private readonly ILogger<ImageValidator> _logger;
    private readonly IStorageService _storageService;
    private readonly IValidationService _validationService;

    public ImageValidator(
        //ILogger<ImageValidator> logger,
        IStorageService storageService,
        IValidationService validationService
    )
    {
        //_logger = logger;
        _storageService = storageService;
        _validationService = validationService;
    }

    [Function("ImageValidator")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        JsonDocument parsedBody;
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        try
        {
          parsedBody = JsonDocument.Parse(requestBody);  
        }
        catch (Exception)
        {
            HttpResponseData res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync("File name was not provided!");
            return res;
        }

        if (
            parsedBody.RootElement.TryGetProperty("file", out JsonElement fileElement) &&
            !string.IsNullOrEmpty(fileElement.GetString()) &&
            !string.IsNullOrWhiteSpace(fileElement.GetString())
            )
        {
            string fileName = fileElement.GetString()!;
            using Stream bytesStream = await _storageService.DownloadFile(fileName);
            bytesStream.Position = 0;

            if (bytesStream != null && bytesStream.CanRead && bytesStream.Length > 0)
            {
                bool isValidImage = _validationService.IsImage(bytesStream);
                if (isValidImage)
                {
                    // Set metadata tag status - valid
                    Dictionary<string, string> statusValid = new Dictionary<string, string>
                    {
                        { Constants.Status, Constants.Valid },
                    };
                    await _storageService.SetMetadataTags(statusValid);
                    HttpResponseData res = req.CreateResponse(HttpStatusCode.OK);
                    await res.WriteStringAsync($"File {fileName} is a valid image");
                    return res;
                }
                else
                {
                    // Delete invalid non-image file
                    await _storageService.DeleteFile(fileName);
                    HttpResponseData res = req.CreateResponse(HttpStatusCode.BadRequest);
                    await res.WriteStringAsync($"File {fileName} is NOT a valid image!");
                    return res;
                }
            }
            else
            {
                HttpResponseData res = req.CreateResponse(HttpStatusCode.BadRequest);
                await res.WriteStringAsync("File not found!");
                return res;
            }
        }
        else
        {
            HttpResponseData res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync("File name was not provided!");
            return res;
        }
    }
}