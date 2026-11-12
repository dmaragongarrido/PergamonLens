using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Application.MediaReleases.Commands.ScanRelease;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.ExternalServices.Games;
using PergamonLens.Infrastructure.ExternalServices.OpenLibrary;

namespace PergamonLens.UnitTests.Entities;

public sealed class BookPolymorphicTests
{
    private static readonly UserId TestUserId = UserId.From("user-books-001");

    [Theory]
    [InlineData(MediaFormat.Vinyl, MediaCategory.Music)]
    [InlineData(MediaFormat.CD, MediaCategory.Music)]
    [InlineData(MediaFormat.Hardcover, MediaCategory.Books)]
    [InlineData(MediaFormat.Paperback, MediaCategory.Books)]
    [InlineData(MediaFormat.ComicManga, MediaCategory.Books)]
    [InlineData(MediaFormat.BookSpecialEdition, MediaCategory.Books)]
    [InlineData(MediaFormat.OpticalDiscGame, MediaCategory.VideoGames)]
    [InlineData(MediaFormat.Cartridge, MediaCategory.VideoGames)]
    public void GetCategory_ShouldReturnExpectedMediaCategory(MediaFormat format, MediaCategory expectedCategory)
    {
        format.GetCategory().Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData("9788497594257", true)]
    [InlineData("9791032305621", true)]
    [InlineData("5099902894523", false)]
    [InlineData("", false)]
    public void IsIsbn_ShouldDetectBooklandPrefixes(string barcode, bool expected)
    {
        MediaFormatExtensions.IsIsbn(barcode).Should().Be(expected);
    }

    [Fact]
    public void CreateBook_ShouldInitializeBookCategoryAndProperties()
    {
        var isbn13 = Barcode.Create("9788497594257");
        var year = ReleaseYear.Create(1951);

        var book = MediaRelease.CreateBook(
            TestUserId,
            title: "Fundación",
            author: "Isaac Asimov",
            format: MediaFormat.Hardcover,
            isbn13: isbn13,
            publishYear: year,
            publisher: "Minotauro",
            pagesCount: 256,
            externalCatalogId: "OL12345M");

        book.Category.Should().Be(MediaCategory.Books);
        book.Format.Should().Be(MediaFormat.Hardcover);
        book.Title.Should().Be("Fundación");
        book.Artist.Should().Be("Isaac Asimov");
        book.Label.Should().Be("Minotauro");
        book.PagesCount.Should().Be(256);
        book.ExternalCatalogId.Should().Be("OL12345M");
        book.VinylSize.Should().BeNull();
        book.VinylSpeed.Should().BeNull();
    }

    [Fact]
    public void EnrichFromBookCatalog_ShouldUpdateMetadataAndSetEnrichedStatus()
    {
        var book = MediaRelease.CreateBook(
            TestUserId,
            title: "Fundacion",
            author: "Asimov",
            format: MediaFormat.Paperback);

        book.EnrichFromBookCatalog(
            editionKey: "OL98765M",
            title: "Fundación (Trilogía de la Fundación)",
            author: "Isaac Asimov",
            publisher: "Debolsillo",
            publishYear: ReleaseYear.Create(2003),
            pagesCount: 320,
            coverImageUrl: "https://covers.openlibrary.org/b/id/12345-L.jpg",
            isbn: CatalogueNumber.Create("9788497594257"));

        book.Status.Should().Be(ProcessingStatus.Enriched);
        book.ExternalCatalogId.Should().Be("OL98765M");
        book.Title.Should().Be("Fundación (Trilogía de la Fundación)");
        book.Artist.Should().Be("Isaac Asimov");
        book.Label.Should().Be("Debolsillo");
        book.PagesCount.Should().Be(320);
        book.CoverImageUrl.Should().Be("https://covers.openlibrary.org/b/id/12345-L.jpg");
    }

    [Fact]
    public async Task ScanReleaseCommandHandler_WhenIsbnBarcodeDetected_ShouldCreateAndEnrichBookViaOpenLibrary()
    {
        var visionMock = new Mock<IVisionService>();
        var repoMock = new Mock<IMediaReleaseRepository>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var discogsMock = new Mock<IDiscogsService>();
        var bookCatalogMock = new Mock<IBookCatalogService>();

        currentUserMock.Setup(x => x.UserId).Returns(TestUserId.Value);

        visionMock
            .Setup(x => x.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionExtractionResult(
                DetectedTitle: null,
                DetectedArtist: null,
                DetectedBarcode: "9788497594257",
                DetectedCatalogueNumber: null,
                DetectedLabel: null,
                Confidence: 1.0,
                Category: PhysicalMediaCategory.Book));

        bookCatalogMock
            .Setup(x => x.LookupByIsbnAsync("9788497594257", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookCatalogSearchResult(
                EditionKey: "OL777M",
                Title: "Fundación",
                Author: "Isaac Asimov",
                PublishYear: 1951,
                Publisher: "Debolsillo",
                Isbn: "9788497594257",
                PagesCount: 256,
                CoverImageUrl: "https://covers.openlibrary.org/b/id/777-L.jpg"));

        var handler = new ScanReleaseCommandHandler(
            visionMock.Object,
            repoMock.Object,
            currentUserMock.Object,
            discogsMock.Object,
            logger: null,
            bookCatalogService: bookCatalogMock.Object);

        using var stream = new MemoryStream([1, 2, 3]);
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl);

        var dto = await handler.Handle(command, CancellationToken.None);

        dto.Category.Should().Be("Books");
        dto.Title.Should().Be("Fundación");
        dto.Artist.Should().Be("Isaac Asimov");
        dto.Label.Should().Be("Debolsillo");
        dto.PagesCount.Should().Be(256);
        dto.ExternalCatalogId.Should().Be("OL777M");
        dto.Status.Should().Be("Enriched");

        repoMock.Verify(x => x.AddAsync(It.Is<MediaRelease>(r => r.Category == MediaCategory.Books), It.IsAny<CancellationToken>()), Times.Once);
        discogsMock.Verify(x => x.SearchByBarcodeAsync(It.IsAny<Barcode>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OpenLibraryService_LookupByIsbnAsync_ShouldParseSearchDocsCorrectly()
    {
        const string sampleJson = """
        {
          "numFound": 1,
          "docs": [
            {
              "key": "/works/OL46125W",
              "title": "Foundation",
              "author_name": ["Isaac Asimov"],
              "first_publish_year": 1951,
              "publisher": ["Gnome Press"],
              "isbn": ["9788497594257"],
              "number_of_pages_median": 255,
              "cover_i": 8231456,
              "edition_key": ["OL243456M"]
            }
          ]
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            });

        using var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };

        var service = new OpenLibraryService(httpClient);
        var result = await service.LookupByIsbnAsync("978-84-9759-425-7");

        result.Should().NotBeNull();
        result!.EditionKey.Should().Be("OL243456M");
        result.Title.Should().Be("Foundation");
        result.Author.Should().Be("Isaac Asimov");
        result.PublishYear.Should().Be(1951);
        result.Publisher.Should().Be("Gnome Press");
        result.PagesCount.Should().Be(255);
        result.CoverImageUrl.Should().Be("https://covers.openlibrary.org/b/id/8231456-L.jpg");
    }

    [Fact]
    public async Task OpenLibraryService_WhenSubjectIsSpanishPoetry_ShouldNormalizeToPoesiaAndReturnNullOverviewWhenNoFirstSentence()
    {
        const string sampleJson = """
        {
          "numFound": 1,
          "docs": [
            {
              "key": "/works/OL9999W",
              "title": "Romancero Gitano",
              "author_name": ["Federico García Lorca"],
              "first_publish_year": 1928,
              "edition_key": ["OL9999M"],
              "subject": ["Accessible book", "Spanish poetry", "20th century"]
            }
          ]
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            });

        using var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };

        var service = new OpenLibraryService(httpClient);
        var result = await service.LookupByIsbnAsync("978-84-376-0114-4");

        result.Should().NotBeNull();
        result!.Genre.Should().Be("Poesía"); // Spanish poetry normalizado a Poesía
        result.Overview.Should().BeNull(); // Cero frases robóticas
    }

    [Fact]
    public void CreateVideoGame_And_EnrichFromGameCatalog_ShouldSetVideoGamePropertiesAndStatus()
    {
        var game = MediaRelease.CreateVideoGame(
            TestUserId,
            title: "Bloodborne",
            developerOrStudio: "FromSoftware",
            format: MediaFormat.OpticalDiscGame,
            platform: "PlayStation 4",
            publisher: "Sony Computer Entertainment");

        game.Category.Should().Be(MediaCategory.VideoGames);
        game.Format.Should().Be(MediaFormat.OpticalDiscGame);
        game.Platform.Should().Be("PlayStation 4");
        game.VinylSize.Should().BeNull();

        game.EnrichFromGameCatalog(
            gameKey: "WIKI-43210",
            title: "Bloodborne (Game of the Year Edition)",
            developer: "FromSoftware",
            publisher: "Sony Interactive Entertainment",
            releaseYear: ReleaseYear.Create(2015),
            platform: "PlayStation 4",
            coverImageUrl: "https://upload.wikimedia.org/wikipedia/en/6/68/Bloodborne_Cover_Wallpaper.jpg");

        game.Status.Should().Be(ProcessingStatus.Enriched);
        game.ExternalCatalogId.Should().Be("WIKI-43210");
        game.Title.Should().Be("Bloodborne (Game of the Year Edition)");
        game.ReleaseYear!.Value.Should().Be(2015);
    }

    [Fact]
    public async Task ScanReleaseCommandHandler_WhenVideoGameDetected_ShouldCreateAndEnrichGame()
    {
        var visionMock = new Mock<IVisionService>();
        var repoMock = new Mock<IMediaReleaseRepository>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var discogsMock = new Mock<IDiscogsService>();
        var gameCatalogMock = new Mock<IGameCatalogService>();

        currentUserMock.Setup(x => x.UserId).Returns(TestUserId.Value);

        visionMock
            .Setup(x => x.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisionExtractionResult(
                DetectedTitle: "The Legend of Zelda: Ocarina of Time",
                DetectedArtist: "Nintendo EAD",
                DetectedBarcode: null,
                DetectedCatalogueNumber: "NUS-CZLE-USA",
                DetectedLabel: "Nintendo",
                Confidence: 0.96,
                Category: PhysicalMediaCategory.VideoGame,
                DetectedFormat: "Cartridge",
                DetectedPlatform: "Nintendo 64",
                DetectedReleaseYear: 1998));

        gameCatalogMock
            .Setup(x => x.SearchByQueryAsync(
                "The Legend of Zelda: Ocarina of Time",
                "Nintendo 64",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new GameCatalogSearchResult(
                    GameKey: "WIKI-999",
                    Title: "The Legend of Zelda: Ocarina of Time",
                    Developer: "Nintendo EAD",
                    ReleaseYear: 1998,
                    Publisher: "Nintendo",
                    Platform: "Nintendo 64",
                    CoverImageUrl: "https://upload.wikimedia.org/wikipedia/en/5/57/The_Legend_of_Zelda_Ocarina_of_Time.jpg")
            ]);

        var handler = new ScanReleaseCommandHandler(
            visionMock.Object,
            repoMock.Object,
            currentUserMock.Object,
            discogsMock.Object,
            logger: null,
            bookCatalogService: null,
            gameCatalogService: gameCatalogMock.Object);

        using var stream = new MemoryStream([9, 8, 7]);
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl);

        var dto = await handler.Handle(command, CancellationToken.None);

        dto.Category.Should().Be("VideoGames");
        dto.Format.Should().Be("Cartridge");
        dto.Platform.Should().Be("Nintendo 64");
        dto.ExternalCatalogId.Should().Be("WIKI-999");
        dto.Status.Should().Be("Enriched");

        repoMock.Verify(x => x.AddAsync(It.Is<MediaRelease>(r => r.Category == MediaCategory.VideoGames), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenGameCatalogService_SearchByFreeTextAsync_ShouldParseMediaWikiGameResults()
    {
        const string sampleWikiJson = """
        {
          "query": {
            "pages": {
              "12345": {
                "pageid": 12345,
                "index": 1,
                "title": "Bloodborne (video game)",
                "description": "2015 action role-playing video game",
                "extract": "Bloodborne is a 2015 action role-playing game developed by FromSoftware and published by Sony Computer Entertainment for the PlayStation 4.",
                "thumbnail": {
                  "source": "https://upload.wikimedia.org/wikipedia/en/6/68/Bloodborne_Cover_Wallpaper.jpg"
                }
              }
            }
          }
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sampleWikiJson, Encoding.UTF8, "application/json")
            });

        using var httpClient = new HttpClient(handlerMock.Object);
        var service = new OpenGameCatalogService(httpClient);

        var results = await service.SearchByFreeTextAsync("Bloodborne PlayStation 4");

        results.Should().NotBeEmpty();
        var first = results[0];
        first.GameKey.Should().Be("WIKI-12345");
        first.Title.Should().Be("Bloodborne");
        first.Developer.Should().Be("FromSoftware");
        first.Publisher.Should().Be("Sony Computer Entertainment");
        first.ReleaseYear.Should().Be(2015);
        first.Platform.Should().Be("PlayStation 4");
        first.CoverImageUrl.Should().Contain("Bloodborne_Cover_Wallpaper.jpg");
    }
}

