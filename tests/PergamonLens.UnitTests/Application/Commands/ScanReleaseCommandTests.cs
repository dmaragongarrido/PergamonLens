using FluentAssertions;
using Moq;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Application.MusicReleases.Commands.ScanRelease;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Commands;

public class ScanReleaseCommandTests
{
    private readonly Mock<IVisionService> _visionServiceMock;
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDiscogsService> _discogsServiceMock;
    private readonly ScanReleaseCommandHandler _handler;
    private readonly ScanReleaseCommandValidator _validator;

    public ScanReleaseCommandTests()
    {
        _visionServiceMock = new Mock<IVisionService>();
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns("default-test-user");
        _discogsServiceMock = new Mock<IDiscogsService>();

        _handler = new ScanReleaseCommandHandler(
            _visionServiceMock.Object,
            _repositoryMock.Object,
            _currentUserServiceMock.Object,
            _discogsServiceMock.Object);

        _validator = new ScanReleaseCommandValidator();
    }

    [Fact]
    public async Task Handle_WhenVisionRecognizesAlbum_ShouldCreateIdentifiedRelease()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl);

        var visionResult = new VisionExtractionResult(
            DetectedTitle: "A Love Supreme",
            DetectedArtist: "John Coltrane",
            DetectedBarcode: "036000291452",
            DetectedCatalogueNumber: "AS-77",
            DetectedLabel: "Impulse!",
            Confidence: 0.95);

        _visionServiceMock
            .Setup(v => v.AnalyzeImageAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("A Love Supreme");
        result.Artist.Should().Be("John Coltrane");
        result.Barcode.Should().Be("036000291452");
        result.CatalogueNumber.Should().Be("AS-77");
        result.Label.Should().Be("Impulse!");
        result.Status.Should().Be("Identified");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<MusicRelease>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVisionRecognizesAlbumWithoutBarcode_ShouldAutoQueryDiscogsAndEnrich()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl);

        var visionResult = new VisionExtractionResult(
            DetectedTitle: "A Night at the Opera",
            DetectedArtist: "Queen",
            DetectedBarcode: null,
            DetectedCatalogueNumber: null,
            DetectedLabel: "EMI",
            Confidence: 0.95);

        _visionServiceMock
            .Setup(v => v.AnalyzeImageAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResult);

        var discogsSearchResults = new List<DiscogsSearchResult>
        {
            new(
                ReleaseId: 78910,
                Title: "A Night at the Opera",
                Artist: "Queen",
                Year: 1975,
                Label: "EMI",
                CatalogueNumber: "EMTC 103",
                Country: "UK",
                CoverThumbUrl: "https://discogs.com/thumb.jpg",
                Format: "Vinyl")
        };

        var discogsDetails = new DiscogsReleaseDetails(
            ReleaseId: 78910,
            Title: "A Night at the Opera",
            Artist: "Queen",
            Year: 1975,
            Label: "EMI",
            CatalogueNumber: "EMTC 103",
            Country: "UK",
            CoverImageUrl: "https://discogs.com/queen_full.jpg",
            Tracks: new List<DiscogsTrackItem>
            {
                new("A1", "Death on Two Legs", TimeSpan.FromMinutes(3.7))
            },
            Format: "Vinyl");

        _discogsServiceMock
            .Setup(d => d.SearchByQueryAsync("Queen", "A Night at the Opera", It.IsAny<CancellationToken>()))
            .ReturnsAsync(discogsSearchResults);

        _discogsServiceMock
            .Setup(d => d.GetReleaseDetailsAsync(78910, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discogsDetails);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("A Night at the Opera");
        result.Artist.Should().Be("Queen");
        result.CoverImageUrl.Should().Be("https://discogs.com/queen_full.jpg");
        result.Tracks.Should().HaveCount(1);
        result.Status.Should().Be("Enriched");

        _discogsServiceMock.Verify(d => d.SearchByQueryAsync("Queen", "A Night at the Opera", It.IsAny<CancellationToken>()), Times.Once);
        _discogsServiceMock.Verify(d => d.GetReleaseDetailsAsync(78910, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBarcodeDetectedInPhase1_ShouldAutoQueryAndEnrichFromDiscogs()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl);
        const string barcodeValue = "4006381333931";

        // Fase 1: solo código de barras, sin título/artista
        var visionResult = new VisionExtractionResult(
            DetectedTitle: null,
            DetectedArtist: null,
            DetectedBarcode: barcodeValue,
            DetectedCatalogueNumber: null,
            DetectedLabel: null,
            Confidence: 1.0);

        _visionServiceMock
            .Setup(v => v.AnalyzeImageAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResult);

        var searchResult = new DiscogsSearchResult(
            ReleaseId: 123456,
            Title: "The Dark Side of the Moon",
            Artist: "Pink Floyd",
            Year: 1973,
            Label: "Harvest",
            CatalogueNumber: "SHVL 804",
            Country: "UK",
            CoverThumbUrl: "https://discogs.com/thumb.jpg");

        _discogsServiceMock
            .Setup(d => d.SearchByBarcodeAsync(It.Is<Barcode>(b => b.Value == barcodeValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscogsSearchResult> { searchResult });

        var releaseDetails = new DiscogsReleaseDetails(
            ReleaseId: 123456,
            Title: "The Dark Side of the Moon",
            Artist: "Pink Floyd",
            Year: 1973,
            Label: "Harvest",
            CatalogueNumber: "SHVL 804",
            Country: "UK",
            CoverImageUrl: "https://discogs.com/full.jpg",
            Tracks: new List<DiscogsTrackItem>
            {
                new("A1", "Speak to Me", TimeSpan.FromSeconds(67)),
                new("A2", "Breathe", TimeSpan.FromSeconds(163))
            });

        _discogsServiceMock
            .Setup(d => d.GetReleaseDetailsAsync(123456, It.IsAny<CancellationToken>()))
            .ReturnsAsync(releaseDetails);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("The Dark Side of the Moon");
        result.Artist.Should().Be("Pink Floyd");
        result.Barcode.Should().Be(barcodeValue);
        result.Label.Should().Be("Harvest");
        result.ReleaseYear.Should().Be(1973);
        result.CoverImageUrl.Should().Be("https://discogs.com/full.jpg");
        result.Status.Should().Be("Enriched");
        result.Tracks.Should().HaveCount(2);

        _discogsServiceMock.Verify(d => d.SearchByBarcodeAsync(It.IsAny<Barcode>(), It.IsAny<CancellationToken>()), Times.Once);
        _discogsServiceMock.Verify(d => d.GetReleaseDetailsAsync(123456, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDiscogsIndicatesCdFormat_ShouldAutoSelectCdFormat()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl); // Usuario o frontend envió Vinyl por defecto
        const string barcodeValue = "4006381333931";

        var visionResult = new VisionExtractionResult(
            DetectedTitle: null,
            DetectedArtist: null,
            DetectedBarcode: barcodeValue,
            DetectedCatalogueNumber: null,
            DetectedLabel: null,
            Confidence: 1.0);

        _visionServiceMock
            .Setup(v => v.AnalyzeImageAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResult);

        var searchResult = new DiscogsSearchResult(
            ReleaseId: 7890,
            Title: "Brothers in Arms",
            Artist: "Dire Straits",
            Year: 1985,
            Label: "Vertigo",
            CatalogueNumber: "824 499-2",
            Country: "Europe",
            CoverThumbUrl: null,
            Format: "CD");

        _discogsServiceMock
            .Setup(d => d.SearchByBarcodeAsync(It.Is<Barcode>(b => b.Value == barcodeValue), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiscogsSearchResult> { searchResult });

        var releaseDetails = new DiscogsReleaseDetails(
            ReleaseId: 7890,
            Title: "Brothers in Arms",
            Artist: "Dire Straits",
            Year: 1985,
            Label: "Vertigo",
            CatalogueNumber: "824 499-2",
            Country: "Europe",
            CoverImageUrl: null,
            Tracks: new List<DiscogsTrackItem>(),
            Format: "CD");

        _discogsServiceMock
            .Setup(d => d.GetReleaseDetailsAsync(7890, It.IsAny<CancellationToken>()))
            .ReturnsAsync(releaseDetails);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("CD");
    }

    [Fact]
    public async Task Handle_WhenConfidenceIsLowAndNoMetadata_ShouldMarkReleaseAsFailed()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
        var command = new ScanReleaseCommand(stream, "image/jpeg", MediaFormat.Vinyl);

        var visionResult = new VisionExtractionResult(
            DetectedTitle: null,
            DetectedArtist: null,
            DetectedBarcode: null,
            DetectedCatalogueNumber: null,
            DetectedLabel: null,
            Confidence: 0.1);

        _visionServiceMock
            .Setup(v => v.AnalyzeImageAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Validator_WhenStreamIsNull_ShouldHaveValidationError()
    {
        var command = new ScanReleaseCommand(null!, "image/jpeg", MediaFormat.Vinyl);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageStream");
    }

    [Fact]
    public async Task Validator_WhenContentTypeIsInvalid_ShouldHaveValidationError()
    {
        using var stream = new MemoryStream(new byte[] { 0x01 });
        var command = new ScanReleaseCommand(stream, "text/plain", MediaFormat.Vinyl);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }
}
