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

    public ImageValidator(
        ILogger<ImageValidator> logger,
        IStorageService storageService
    )
    {
        _logger = logger;
        _storageService = storageService;
    }

    [Function("ImageValidator")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}