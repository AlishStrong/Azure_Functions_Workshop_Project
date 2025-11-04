using System.Net;
using System.Text;
using ImageProcessor.Common;
using ImageProcessor.Services;
using ImageProcessor.Tests.TestHelpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ImageProcessor.Tests.Functions;

public class ImageValidator_Tests
{
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IValidationService> _validationServiceMock;

    private readonly Mock<HttpRequestData> _requestDataMock;
    private readonly Mock<HttpResponseData> _responseDataMock;

    // SUT
    private readonly ImageValidator _imageValidator;

    // Storage imitations for unit tests
    private readonly List<StorageBlob> _inboundBlobs = [];
    private readonly List<StorageBlob> _deletedBlobs = [];

    public ImageValidator_Tests()
    {
        _storageServiceMock = new();
        _validationServiceMock = new();

        Mock<FunctionContext> functionContextMock = new();
        _requestDataMock = new(functionContextMock.Object);
        _responseDataMock = new(functionContextMock.Object);

        _requestDataMock.Setup(r => r.CreateResponse()).Returns(_responseDataMock.Object);
        _responseDataMock.SetupProperty(r => r.StatusCode);
        _responseDataMock.Setup(r => r.Body).Returns(new MemoryStream());

        _imageValidator = new ImageValidator(
            _storageServiceMock.Object,
            _validationServiceMock.Object
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("name")]
    [InlineData($@"{{ ""file"": """" }}")]
    [InlineData($@"{{ ""file"": ""  "" }}")]
    [InlineData($@"{{ ""file"": null }}")]
    [InlineData($@"{{ ""feli"": ""name"" }}")]
    public async Task Should_return_BadRequest_if_HttpRequest_Body_isIncorrect(string? requestBody)
    {
        if (requestBody == null)
        {
            _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream());
        }
        else
        {
            _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestBody)));
        }

        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal("File name was not provided!", responseBody);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Should_return_BadRequest_if_file_not_in_Storage()
    {
        _storageServiceMock.Setup(s => s.DownloadFile(It.IsAny<string>())).ReturnsAsync(new MemoryStream());
        _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes($@"{{ ""file"": ""name"" }}")));
        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal("File not found!", responseBody);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Should_return_BadRequest_and_delete_file_if_too_big()
    {
        // Arrange
        _inboundBlobs.Clear();
        _deletedBlobs.Clear();

        string blobName = "huge";
        StorageBlob fileBlob = new(blobName);
        _inboundBlobs.Add(fileBlob);

        byte[] fileBytes = Encoding.UTF8.GetBytes("too much");
        _storageServiceMock.Setup(s => s.DownloadFile(It.IsAny<string>())).ReturnsAsync(new MemoryStream(fileBytes));
        _storageServiceMock
            .Setup(s => s.DeleteFile(It.IsAny<string>()))
            .Callback<string>(s =>
            {
                _inboundBlobs.Remove(fileBlob);
                _deletedBlobs.Add(fileBlob);
            });
        _validationServiceMock.Setup(v => v.IsValidSize(It.IsAny<Stream>())).Returns(false);
        _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes($@"{{ ""file"": ""{fileBlob.Name}"" }}")));

        // Act
        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        // Assert
        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal($"File {blobName} is too big!", responseBody);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Should_return_BadRequest_and_delete_file_if_file_not_image()
    {
        // Arrange
        _inboundBlobs.Clear();
        _deletedBlobs.Clear();

        string blobName = "file";
        StorageBlob fileBlob = new(blobName);
        _inboundBlobs.Add(fileBlob);

        byte[] fileBytes = Encoding.UTF8.GetBytes("does not matter content");
        _storageServiceMock.Setup(s => s.DownloadFile(It.IsAny<string>())).ReturnsAsync(new MemoryStream(fileBytes));
        _storageServiceMock
            .Setup(s => s.DeleteFile(It.IsAny<string>()))
            .Callback<string>(s =>
            {
                _inboundBlobs.Remove(fileBlob);
                _deletedBlobs.Add(fileBlob);
            });
        _validationServiceMock.Setup(v => v.IsValidSize(It.IsAny<Stream>())).Returns(true);
        _validationServiceMock.Setup(v => v.IsImage(It.IsAny<Stream>())).Returns(false);
        _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes($@"{{ ""file"": ""{fileBlob.Name}"" }}")));
        
        // Act
        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        // Assert
        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal($"File {fileBlob.Name} is NOT a valid image!", responseBody);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        Assert.Empty(_inboundBlobs);
        Assert.NotEmpty(_deletedBlobs);
        Assert.Equal(blobName, _deletedBlobs.First().Name);
    }
    
    [Fact]
    public async Task Should_return_OK_and_set_StatusValid_if_file_is_image()
    {
        // Arrange
        _inboundBlobs.Clear();
        _deletedBlobs.Clear();

        string blobName = "image";
        StorageBlob imageBlob = new(blobName);
        _inboundBlobs.Add(imageBlob);

        byte[] fileBytes = Encoding.UTF8.GetBytes("does not matter content");
        _storageServiceMock.Setup(s => s.DownloadFile(It.IsAny<string>())).ReturnsAsync(new MemoryStream(fileBytes));
        _storageServiceMock
            .Setup(s => s.SetMetadataTags(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<string, Dictionary<string, string>>((_, tags) => imageBlob.Status = tags.First().Value);
        _validationServiceMock.Setup(v => v.IsValidSize(It.IsAny<Stream>())).Returns(true);
        _validationServiceMock.Setup(v => v.IsImage(It.IsAny<Stream>())).Returns(true);
        _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes($@"{{ ""file"": ""{imageBlob.Name}"" }}")));
        
        // Act
        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        // Assert
        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal($"File {imageBlob.Name} is a valid image", responseBody);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.Empty(_deletedBlobs);
        Assert.Equal(Constants.Valid, imageBlob.Status);
    }
}
