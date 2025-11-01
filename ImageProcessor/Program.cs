using ImageProcessor.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

string? storageAccountConnectionString = Environment.GetEnvironmentVariable("CUSTOMCONNSTR_AZURE_STORAGE");
string storageContainer = "images";

if (storageAccountConnectionString != null)
{
    BlobContainerClient imagesContainerClient = new BlobContainerClient(storageAccountConnectionString, storageContainer);
    builder.Services.AddSingleton(imagesContainerClient);
} else
{
    Console.WriteLine("Azure Blob Storage Account connection string is missing");
}

builder.Services.AddSingleton<IStorageService, StorageService>();

builder.Build().Run();
