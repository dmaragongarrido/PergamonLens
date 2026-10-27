using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PergamonLens.Domain.Services;
using PergamonLens.Infrastructure.ExternalServices.Vision;

namespace PergamonLens.UnitTests.Infrastructure;

public sealed class VisionServiceTests
{
    private readonly Mock<IBarcodeScanner> _barcodeScannerMock;
    private readonly Mock<IGeminiVisionClient> _geminiClientMock;
    private readonly IOptions<VisionOptions> _options;
    private readonly VisionService _sut;

    public VisionServiceTests()
    {
        _barcodeScannerMock = new Mock<IBarcodeScanner>();
        _geminiClientMock = new Mock<IGeminiVisionClient>();
        _options = Options.Create(new VisionOptions
        {
            DefaultConfidence = 0.90
        });

        _sut = new VisionService(
            _barcodeScannerMock.Object,
            _geminiClientMock.Object,
            _options,
            NullLogger<VisionService>.Instance);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.AnalyzeImageAsync(null!, "image/jpeg");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AnalyzeImageAsync_WithInvalidContentType_ShouldThrowArgumentException(string? invalidContentType)
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var act = () => _sut.AnalyzeImageAsync(stream, invalidContentType!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenMusicBarcodeDetectedInPhase1_ShouldReturnMusicCategoryAndOmitCloudAI()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);
        const string barcode = "5099902894512";
        const string format = "EAN_13";

        _barcodeScannerMock
            .Setup(s => s.Scan(It.IsAny<Stream>()))
            .Returns(new BarcodeScanResult(barcode, format));

        // Act
        var result = await _sut.AnalyzeImageAsync(stream, "image/png");

        // Assert
        result.Should().NotBeNull();
        result.DetectedBarcode.Should().Be(barcode);
        result.Confidence.Should().Be(1.0);
        result.Category.Should().Be(PhysicalMediaCategory.Music);

        _barcodeScannerMock.Verify(s => s.Scan(It.IsAny<Stream>()), Times.Once);
        _geminiClientMock.Verify(g => g.AnalyzeMediaCoverAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenBooklandIsbnBarcodeDetectedInPhase1_ShouldReturnBookCategoryAndOmitCloudAI()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);
        const string isbnBarcode = "9788445077528"; // El Señor de los Anillos (Bookland 978)
        const string format = "EAN_13";

        _barcodeScannerMock
            .Setup(s => s.Scan(It.IsAny<Stream>()))
            .Returns(new BarcodeScanResult(isbnBarcode, format));

        // Act
        var result = await _sut.AnalyzeImageAsync(stream, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.DetectedBarcode.Should().Be(isbnBarcode);
        result.Confidence.Should().Be(1.0);
        result.Category.Should().Be(PhysicalMediaCategory.Book);

        _barcodeScannerMock.Verify(s => s.Scan(It.IsAny<Stream>()), Times.Once);
        _geminiClientMock.Verify(g => g.AnalyzeMediaCoverAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenNoBarcode_AndGeminiDetectsVinylCover_ShouldReturnEnrichedMusicResult()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);

        _barcodeScannerMock
            .Setup(s => s.Scan(It.IsAny<Stream>()))
            .Returns((BarcodeScanResult?)null);

        var geminiResponse = new GeminiVisionAnalysisDto(
            MediaCategory: "Music",
            Title: "The Dark Side of the Moon",
            Creator: "Pink Floyd",
            Format: "Vinyl LP",
            Platform: null,
            ReleaseYear: 1973,
            PublisherOrLabel: "Harvest",
            CatalogueOrIdentifier: "SHVL 804",
            Barcode: null,
            Confidence: 0.98,
            SuggestedSearchQuery: "Pink Floyd The Dark Side of the Moon 1973");

        _geminiClientMock
            .Setup(g => g.AnalyzeMediaCoverAsync(It.IsAny<Stream>(), "image/jpeg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(geminiResponse);

        // Act
        var result = await _sut.AnalyzeImageAsync(stream, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be(PhysicalMediaCategory.Music);
        result.DetectedTitle.Should().Be("The Dark Side of the Moon");
        result.DetectedArtist.Should().Be("Pink Floyd");
        result.DetectedFormat.Should().Be("Vinyl LP");
        result.DetectedReleaseYear.Should().Be(1973);
        result.DetectedLabel.Should().Be("Harvest");
        result.Confidence.Should().Be(0.98);
        result.SuggestedSearchQuery.Should().Be("Pink Floyd The Dark Side of the Moon 1973");
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenNoBarcode_AndGeminiDetectsVideoGame_ShouldReturnVideoGameResultWithPlatform()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);

        _barcodeScannerMock
            .Setup(s => s.Scan(It.IsAny<Stream>()))
            .Returns((BarcodeScanResult?)null);

        var geminiResponse = new GeminiVisionAnalysisDto(
            MediaCategory: "VideoGame",
            Title: "The Legend of Zelda: Ocarina of Time",
            Creator: "Nintendo EAD",
            Format: "Cartridge",
            Platform: "Nintendo 64",
            ReleaseYear: 1998,
            PublisherOrLabel: "Nintendo",
            CatalogueOrIdentifier: "NUS-CZLE-USA",
            Barcode: "045496870027",
            Confidence: 0.99,
            SuggestedSearchQuery: "The Legend of Zelda Ocarina of Time Nintendo 64 1998");

        _geminiClientMock
            .Setup(g => g.AnalyzeMediaCoverAsync(It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(geminiResponse);

        // Act
        var result = await _sut.AnalyzeImageAsync(stream, "image/png");

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be(PhysicalMediaCategory.VideoGame);
        result.DetectedTitle.Should().Be("The Legend of Zelda: Ocarina of Time");
        result.DetectedArtist.Should().Be("Nintendo EAD");
        result.DetectedCreator.Should().Be("Nintendo EAD");
        result.DetectedPlatform.Should().Be("Nintendo 64");
        result.DetectedFormat.Should().Be("Cartridge");
        result.Confidence.Should().Be(0.99);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenGeminiFails_ShouldGracefullyFallbackWithoutThrowing()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3, 4]);

        _barcodeScannerMock
            .Setup(s => s.Scan(It.IsAny<Stream>()))
            .Returns((BarcodeScanResult?)null);

        _geminiClientMock
            .Setup(g => g.AnalyzeMediaCoverAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeminiVisionAnalysisDto?)null);

        // Act
        var result = await _sut.AnalyzeImageAsync(stream, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.DetectedTitle.Should().BeNull();
        result.Confidence.Should().Be(_options.Value.DefaultConfidence);
        result.RawTextDetected.Should().Contain("fallback");
    }
}
