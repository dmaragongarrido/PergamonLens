using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.ExternalServices.Discogs;

namespace PergamonLens.UnitTests.Infrastructure;

public class DiscogsServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IOptions<DiscogsOptions> _options;
    private readonly Mock<ILogger<DiscogsService>> _loggerMock;
    private readonly DiscogsService _service;

    public DiscogsServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.discogs.com/")
        };

        _options = Options.Create(new DiscogsOptions
        {
            BaseUrl = "https://api.discogs.com/",
            UserAgent = "TestAgent",
            Token = "TestToken"
        });

        _loggerMock = new Mock<ILogger<DiscogsService>>();
        _service = new DiscogsService(_httpClient, _options, _loggerMock.Object);
    }

    [Fact]
    public async Task SearchByBarcodeAsync_WhenApiReturnsResults_ShouldMapToDomainDtos()
    {
        // Arrange
        var jsonResponse = """
        {
            "results": [
                {
                    "id": 123456,
                    "title": "Pink Floyd - The Dark Side of the Moon",
                    "year": "1973",
                    "label": ["Harvest"],
                    "catno": "SHVL 804",
                    "country": "UK",
                    "thumb": "https://img.discogs.com/thumb.jpg"
                }
            ]
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);
        var barcode = Barcode.Create("5099902987613");

        // Act
        var results = await _service.SearchByBarcodeAsync(barcode, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
        var item = results[0];
        item.ReleaseId.Should().Be(123456);
        item.Artist.Should().Be("Pink Floyd");
        item.Title.Should().Be("The Dark Side of the Moon");
        item.Year.Should().Be(1973);
        item.Label.Should().Be("Harvest");
        item.CatalogueNumber.Should().Be("SHVL 804");
        item.Country.Should().Be("UK");
    }

    [Fact]
    public async Task SearchByCatalogueNumberAsync_WhenApiReturnsResults_ShouldMapToDomainDtos()
    {
        // Arrange
        var jsonResponse = """
        {
            "results": [
                {
                    "id": 99999,
                    "title": "Miles Davis - Kind of Blue",
                    "year": "1959",
                    "label": ["Columbia"],
                    "catno": "CL 1355",
                    "country": "US",
                    "thumb": "https://img.discogs.com/thumb.jpg"
                }
            ]
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);
        var catNo = CatalogueNumber.Create("CL 1355");

        // Act
        var results = await _service.SearchByCatalogueNumberAsync(catNo, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].Artist.Should().Be("Miles Davis");
        results[0].Title.Should().Be("Kind of Blue");
        results[0].Year.Should().Be(1959);
    }

    [Fact]
    public async Task SearchByFreeTextAsync_WhenApiReturnsResults_ShouldMapToDomainDtos()
    {
        // Arrange
        var jsonResponse = """"""
        {
            "results": [
                {
                    "id": 88888,
                    "title": "Queen - A Night at the Opera",
                    "year": "1975",
                    "label": ["EMI"],
                    "catno": "EMTC 103",
                    "country": "UK",
                    "thumb": "https://img.discogs.com/opera.jpg"
                }
            ]
        }
        """""";

        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var results = await _service.SearchByFreeTextAsync("Queen Opera", CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].Artist.Should().Be("Queen");
        results[0].Title.Should().Be("A Night at the Opera");
        results[0].Year.Should().Be(1975);
        results[0].Label.Should().Be("EMI");
    }

    [Fact]
    public async Task GetReleaseDetailsAsync_WhenReleaseExists_ShouldMapTracksAndMetadata()
    {
        // Arrange
        var jsonResponse = """
        {
            "id": 249504,
            "title": "Abbey Road",
            "artists": [
                { "name": "The Beatles" }
            ],
            "year": 1969,
            "labels": [
                { "name": "Apple Records", "catno": "PCS 7088" }
            ],
            "country": "UK",
            "images": [
                { "uri": "https://img.discogs.com/abbey.jpg" }
            ],
            "tracklist": [
                { "position": "A1", "title": "Come Together", "duration": "4:20" },
                { "position": "A2", "title": "Something", "duration": "3:03" }
            ]
        }
        """;

        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var details = await _service.GetReleaseDetailsAsync(249504, CancellationToken.None);

        // Assert
        details.Should().NotBeNull();
        details!.ReleaseId.Should().Be(249504);
        details.Title.Should().Be("Abbey Road");
        details.Artist.Should().Be("The Beatles");
        details.Year.Should().Be(1969);
        details.Label.Should().Be("Apple Records");
        details.CatalogueNumber.Should().Be("PCS 7088");
        details.Country.Should().Be("UK");
        details.CoverImageUrl.Should().Be("https://img.discogs.com/abbey.jpg");
        details.Tracks.Should().HaveCount(2);

        var t1 = details.Tracks[0];
        t1.Position.Should().Be("A1");
        t1.Title.Should().Be("Come Together");
        t1.Duration.Should().Be(TimeSpan.FromSeconds(260));
    }

    [Fact]
    public async Task GetReleaseDetailsAsync_WhenIdIsInvalid_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetReleaseDetailsAsync(0, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
