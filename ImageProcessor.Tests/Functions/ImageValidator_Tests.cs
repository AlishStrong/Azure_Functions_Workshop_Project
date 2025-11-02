using System.Net;
using System.Text;
using ImageProcessor.Services;
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
    public async Task Should_return_BadRequest_if_file_not_image()
    {
        string fileName = "test-file";
        byte[] fileBytes = Encoding.UTF8.GetBytes("does not matter content");
        _storageServiceMock.Setup(s => s.DownloadFile(It.IsAny<string>())).ReturnsAsync(new MemoryStream(fileBytes));
        _validationServiceMock.Setup(v => v.IsImage(It.IsAny<Stream>())).Returns(false);
        _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes($@"{{ ""file"": ""{fileName}"" }}")));
        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal($"File {fileName} is NOT a valid image!", responseBody);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
    
    [Fact]
    public async Task Should_return_OK_if_file_is_image()
    {
        string fileName = "test-file";
        byte[] fileBytes = Encoding.UTF8.GetBytes("does not matter content");
        _storageServiceMock.Setup(s => s.DownloadFile(It.IsAny<string>())).ReturnsAsync(new MemoryStream(fileBytes));
        _validationServiceMock.Setup(v => v.IsImage(It.IsAny<Stream>())).Returns(true);
        _requestDataMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes($@"{{ ""file"": ""{fileName}"" }}")));
        HttpResponseData res = await _imageValidator.Run(_requestDataMock.Object);

        res.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(res.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal($"File {fileName} is a valid image", responseBody);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
