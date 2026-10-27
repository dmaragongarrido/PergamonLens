using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PergamonLens.Infrastructure.ExternalServices.Vision;

namespace PergamonLens.UnitTests.Infrastructure;

public sealed class GeminiVisionClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<VisionOptions> _options;

    public GeminiVisionClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        _options = Options.Create(new VisionOptions
        {
            ApiKey = "test-gemini-api-key",
            Model = "gemini-flash-lite-latest",
            BaseUrl = "https://generativelanguage.googleapis.com/"
        });
    }

    [Fact]
    public async Task AnalyzeMediaCoverAsync_WithoutApiKey_ShouldReturnNullImmediately()
    {
        // Arrange
        var optionsWithoutKey = Options.Create(new VisionOptions { ApiKey = null, GeminiApiKey = null });
        var sut = new GeminiVisionClient(_httpClient, optionsWithoutKey, NullLogger<GeminiVisionClient>.Instance);
        using var stream = new MemoryStream([1, 2, 3]);

        // Act
        var result = await sut.AnalyzeMediaCoverAsync(stream, "image/jpeg");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeMediaCoverAsync_WhenHttpReturnsInternalServerError_ShouldReturnNull()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server Error")
            });

        var sut = new GeminiVisionClient(_httpClient, _options, NullLogger<GeminiVisionClient>.Instance);
        using var stream = new MemoryStream([1, 2, 3, 4]);

        // Act
        var result = await sut.AnalyzeMediaCoverAsync(stream, "image/jpeg");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeMediaCoverAsync_WhenGeminiReturnsStructuredMusicJson_ShouldParseSuccessfully()
    {
        // Arrange: respuesta JSON canónica de Gemini con candidates y responseSchema
        const string structuredMusicJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "{\n  \"mediaCategory\": \"Music\",\n  \"title\": \"A Love Supreme\",\n  \"creator\": \"John Coltrane\",\n  \"format\": \"Vinyl LP\",\n  \"releaseYear\": 1965,\n  \"publisherOrLabel\": \"Impulse!\",\n  \"catalogueOrIdentifier\": \"AS-77\",\n  \"barcode\": \"011105007718\",\n  \"confidence\": 0.99,\n  \"suggestedSearchQuery\": \"John Coltrane A Love Supreme 1965 Impulse\"\n}"
                  }
                ]
              }
            }
          ]
        }
        """;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("models/gemini-flash-lite-latest:generateContent")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(structuredMusicJson, Encoding.UTF8, "application/json")
            });

        var sut = new GeminiVisionClient(_httpClient, _options, NullLogger<GeminiVisionClient>.Instance);
        using var stream = new MemoryStream([1, 2, 3, 4]);

        // Act
        var result = await sut.AnalyzeMediaCoverAsync(stream, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result!.MediaCategory.Should().Be("Music");
        result.Title.Should().Be("A Love Supreme");
        result.Creator.Should().Be("John Coltrane");
        result.Format.Should().Be("Vinyl LP");
        result.ReleaseYear.Should().Be(1965);
        result.PublisherOrLabel.Should().Be("Impulse!");
        result.CatalogueOrIdentifier.Should().Be("AS-77");
        result.Barcode.Should().Be("011105007718");
        result.Confidence.Should().Be(0.99);
    }

    [Fact]
    public async Task AnalyzeMediaCoverAsync_WhenGeminiReturnsStructuredBookJson_ShouldParseSuccessfully()
    {
        // Arrange
        const string structuredBookJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "{\n  \"mediaCategory\": \"Book\",\n  \"title\": \"Dune\",\n  \"creator\": \"Frank Herbert\",\n  \"format\": \"Hardcover\",\n  \"releaseYear\": 1965,\n  \"publisherOrLabel\": \"Chilton Books\",\n  \"catalogueOrIdentifier\": \"9780441172719\",\n  \"confidence\": 0.97,\n  \"suggestedSearchQuery\": \"Frank Herbert Dune 1965 Chilton Books\"\n}"
                  }
                ]
              }
            }
          ]
        }
        """;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(structuredBookJson, Encoding.UTF8, "application/json")
            });

        var sut = new GeminiVisionClient(_httpClient, _options, NullLogger<GeminiVisionClient>.Instance);
        using var stream = new MemoryStream([1, 2, 3, 4]);

        // Act
        var result = await sut.AnalyzeMediaCoverAsync(stream, "image/png");

        // Assert
        result.Should().NotBeNull();
        result!.MediaCategory.Should().Be("Book");
        result.Title.Should().Be("Dune");
        result.Creator.Should().Be("Frank Herbert");
        result.PublisherOrLabel.Should().Be("Chilton Books");
        result.CatalogueOrIdentifier.Should().Be("9780441172719");
    }

    [Fact]
    public async Task AnalyzeMediaCoverAsync_WhenGeminiReturnsStructuredVideoGameJson_ShouldParseSuccessfully()
    {
        // Arrange
        const string structuredGameJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "{\n  \"mediaCategory\": \"VideoGame\",\n  \"title\": \"Final Fantasy VII\",\n  \"creator\": \"Square\",\n  \"format\": \"Optical Disc\",\n  \"platform\": \"PlayStation\",\n  \"releaseYear\": 1997,\n  \"publisherOrLabel\": \"Sony Computer Entertainment\",\n  \"catalogueOrIdentifier\": \"SCES-00867\",\n  \"confidence\": 0.99,\n  \"suggestedSearchQuery\": \"Final Fantasy VII PlayStation 1997 Square\"\n}"
                  }
                ]
              }
            }
          ]
        }
        """;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(structuredGameJson, Encoding.UTF8, "application/json")
            });

        var sut = new GeminiVisionClient(_httpClient, _options, NullLogger<GeminiVisionClient>.Instance);
        using var stream = new MemoryStream([1, 2, 3, 4]);

        // Act
        var result = await sut.AnalyzeMediaCoverAsync(stream, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result!.MediaCategory.Should().Be("VideoGame");
        result.Title.Should().Be("Final Fantasy VII");
        result.Creator.Should().Be("Square");
        result.Platform.Should().Be("PlayStation");
        result.CatalogueOrIdentifier.Should().Be("SCES-00867");
    }
}

