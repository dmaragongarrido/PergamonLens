using FluentAssertions;
using Moq;
using PergamonLens.Application.MediaReleases.Queries.GetCollection;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Queries;

public class GetCollectionQueryTests
{
    private readonly Mock<IMediaReleaseRepository> _repositoryMock;
    private readonly GetCollectionQueryHandler _handler;

    public GetCollectionQueryTests()
    {
        _repositoryMock = new Mock<IMediaReleaseRepository>();
        _handler = new GetCollectionQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNoParameters_ShouldReturnAllReleases()
    {
        // Arrange
        var r1 = MediaRelease.Create(UserId.New(), "Album 1", "Artist 1");
        var r2 = MediaRelease.Create(UserId.New(), "Album 2", "Artist 2");

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaRelease> { r1, r2 });

        var query = new GetCollectionQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        _repositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchQuery_ShouldCallSearchAsync()
    {
        // Arrange
        var r1 = MediaRelease.Create(UserId.New(), "A Love Supreme", "John Coltrane");

        _repositoryMock
            .Setup(r => r.SearchAsync("Coltrane", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaRelease> { r1 });

        var query = new GetCollectionQuery(SearchQuery: "Coltrane");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("A Love Supreme");
        _repositoryMock.Verify(r => r.SearchAsync("Coltrane", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFormatFilter_ShouldCallGetByFormatAsync()
    {
        // Arrange
        var r1 = MediaRelease.CreateCd(UserId.New(), "OK Computer", "Radiohead");

        _repositoryMock
            .Setup(r => r.GetByFormatAsync(MediaFormat.CD, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaRelease> { r1 });

        var query = new GetCollectionQuery(Format: MediaFormat.CD);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Format.Should().Be("CD");
        _repositoryMock.Verify(r => r.GetByFormatAsync(MediaFormat.CD, It.IsAny<CancellationToken>()), Times.Once);
    }
}
