using FluentAssertions;
using Moq;
using PergamonLens.Application.MusicReleases.Queries.GetCollectionStatistics;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Queries;

public class GetCollectionStatisticsQueryTests
{
    private readonly Mock<ICollectionRepository> _repositoryMock;
    private readonly GetCollectionStatisticsQueryHandler _handler;

    public GetCollectionStatisticsQueryTests()
    {
        _repositoryMock = new Mock<ICollectionRepository>();
        _handler = new GetCollectionStatisticsQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnStatisticsDto()
    {
        // Arrange
        var stats = new CollectionStatistics(
            TotalReleases: 42,
            ConfirmedReleases: 35,
            PendingAnalysisReleases: 5,
            EnrichedReleases: 2,
            TotalTracks: 420,
            TotalEstimatedDuration: TimeSpan.FromHours(30),
            FormatBreakdown: new Dictionary<MediaFormat, int> { [MediaFormat.Vinyl] = 30, [MediaFormat.CD] = 12 },
            ConditionBreakdown: new Dictionary<MediaCondition, int> { [MediaCondition.Mint] = 20 },
            VinylSizeBreakdown: new Dictionary<VinylSize, int> { [VinylSize.Inch12] = 28 });

        _repositoryMock
            .Setup(r => r.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var query = new GetCollectionStatisticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalReleases.Should().Be(42);
        result.ConfirmedReleases.Should().Be(35);
        result.FormatBreakdown.Should().ContainKey("Vinyl").WhoseValue.Should().Be(30);
    }
}

