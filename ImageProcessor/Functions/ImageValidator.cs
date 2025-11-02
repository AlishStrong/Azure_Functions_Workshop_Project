using System.Text.Json;
using ImageProcessor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImageProcessor;

public class ImageValidator
{
    private readonly ILogger<ImageValidator> _logger;
    private readonly IStorageService _storageService;
    private readonly IValidationService _validationService;

    public ImageValidator(
        ILogger<ImageValidator> logger,
        IStorageService storageService,
        IValidationService validationService
    )
    {
        _logger = logger;
        _storageService = storageService;
        _validationService = validationService;
    }

    [Function("ImageValidator")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        using JsonDocument parsedBody = JsonDocument.Parse(requestBody);

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
                    return new OkObjectResult($"File {fileName} is a valid image");
                }
                else
                {
                    return new BadRequestObjectResult($"File {fileName} is NOT a valid image!");
                }
            }
            else
            {
                return new BadRequestObjectResult("File not found!");
            }
        }
        else
        {
            return new BadRequestObjectResult("File name was not provided!");
        }
    }
}