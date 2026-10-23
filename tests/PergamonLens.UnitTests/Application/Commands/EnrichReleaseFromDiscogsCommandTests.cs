using FluentAssertions;
using Moq;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.MusicReleases.Commands.EnrichReleaseFromDiscogs;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Commands;

public class EnrichReleaseFromDiscogsCommandTests
{
    private readonly Mock<IMusicReleaseRepository> _repositoryMock;
    private readonly Mock<IDiscogsService> _discogsServiceMock;
    private readonly EnrichReleaseFromDiscogsCommandHandler _handler;
    private readonly EnrichReleaseFromDiscogsCommandValidator _validator;

    public EnrichReleaseFromDiscogsCommandTests()
    {
        _repositoryMock = new Mock<IMusicReleaseRepository>();
        _discogsServiceMock = new Mock<IDiscogsService>();
        _handler = new EnrichReleaseFromDiscogsCommandHandler(_repositoryMock.Object, _discogsServiceMock.Object);
        _validator = new EnrichReleaseFromDiscogsCommandValidator();
    }

    [Fact]
    public async Task Handle_WhenReleaseAndDiscogsFound_ShouldEnrichAndReturnDto()
    {
        // Arrange
        var release = MusicRelease.Create(UserId.New(), "Abbey Road", "The Beatles", MediaFormat.Vinyl);
        _repositoryMock
            .Setup(r => r.GetByIdAsync(release.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        var discogsTracks = new List<DiscogsTrackItem>
        {
            new("A1", "Come Together", TimeSpan.FromSeconds(260)),
            new("A2", "Something", TimeSpan.FromSeconds(183))
        };

        var discogsDetails = new DiscogsReleaseDetails(
            ReleaseId: 249504,
            Title: "Abbey Road",
            Artist: "The Beatles",
            Year: 1969,
            Label: "Apple Records",
            CatalogueNumber: "PCS 7088",
            Country: "UK",
            CoverImageUrl: "https://example.com/abbey.jpg",
            Tracks: discogsTracks);

        _discogsServiceMock
            .Setup(s => s.GetReleaseDetailsAsync(249504, It.IsAny<CancellationToken>()))
            .ReturnsAsync(discogsDetails);

        var command = new EnrichReleaseFromDiscogsCommand(release.Id.Value, 249504);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Enriched");
        result.DiscogsReleaseId.Should().Be(249504);
        result.Label.Should().Be("Apple Records");
        result.ReleaseYear.Should().Be(1969);
        result.Country.Should().Be("UK");
        result.Tracks.Should().HaveCount(2);

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<MusicRelease>(m =>
            m.Id == release.Id &&
            m.Status == ProcessingStatus.Enriched &&
            m.Tracks.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReleaseNotFoundInRepository_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(new MusicReleaseId(nonExistentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MusicRelease?)null);

        var command = new EnrichReleaseFromDiscogsCommand(nonExistentId, 12345);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenDiscogsDetailsNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var release = MusicRelease.Create(UserId.New(), "Abbey Road", "The Beatles");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(release.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(release);

        _discogsServiceMock
            .Setup(s => s.GetReleaseDetailsAsync(999999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscogsReleaseDetails?)null);

        var command = new EnrichReleaseFromDiscogsCommand(release.Id.Value, 999999);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 12345)]
    [InlineData("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d", 0)]
    [InlineData("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d", -5)]
    public void Validator_WithInvalidParameters_ShouldHaveValidationError(string releaseIdStr, long discogsId)
    {
        // Arrange
        var command = new EnrichReleaseFromDiscogsCommand(Guid.Parse(releaseIdStr), discogsId);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
