using FluentAssertions;
using Moq;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Application.MusicReleases.Commands.ScanRelease;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;

namespace PergamonLens.UnitTests.Application.Commands;

public class ScanReleaseCommandTests
{
    private readonly Mock<IVisionService> _visionServiceMock;
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly ScanReleaseCommandHandler _handler;
    private readonly ScanReleaseCommandValidator _validator;

    public ScanReleaseCommandTests()
    {
        _visionServiceMock = new Mock<IVisionService>();
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns("default-test-user");

        _handler = new ScanReleaseCommandHandler(_visionServiceMock.Object, _repositoryMock.Object, _currentUserServiceMock.Object);
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
        result.UserId.Should().Be("default-test-user");

        _repositoryMock.Verify(r => r.AddAsync(It.Is<MusicRelease>(m =>
            m.Title == "A Love Supreme" &&
            m.UserId.Value == "default-test-user" &&
            m.Status == ProcessingStatus.Identified), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVisionFailsWithLowConfidence_ShouldMarkAsFailed()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        var command = new ScanReleaseCommand(stream, "image/png", MediaFormat.Vinyl);

        var visionResult = new VisionExtractionResult(
            DetectedTitle: null,
            DetectedArtist: null,
            DetectedBarcode: null,
            DetectedCatalogueNumber: null,
            DetectedLabel: null,
            Confidence: 0.05);

        _visionServiceMock
            .Setup(v => v.AnalyzeImageAsync(It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visionResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Failed");
        result.Notes.Should().Contain("Error");
        result.UserId.Should().Be("default-test-user");

        _repositoryMock.Verify(r => r.AddAsync(It.Is<MusicRelease>(m =>
            m.Status == ProcessingStatus.Failed &&
            m.UserId.Value == "default-test-user"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_WithUnsupportedContentType_ShouldHaveValidationError()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x01 });
        var command = new ScanReleaseCommand(stream, "application/pdf");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }
}
