using FluentAssertions;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application;

public class NaturalTrackPositionComparerTests
{
    private readonly NaturalTrackPositionComparer _comparer = NaturalTrackPositionComparer.Instance;

    [Theory]
    [InlineData("1", "2", -1)]
    [InlineData("2", "10", -1)]
    [InlineData("9", "10", -1)]
    [InlineData("10", "11", -1)]
    [InlineData("11", "19", -1)]
    [InlineData("A1", "A2", -1)]
    [InlineData("A9", "A10", -1)]
    [InlineData("A10", "B1", -1)]
    [InlineData("1", "1.2", -1)]
    [InlineData("1.2", "1.3", -1)]
    [InlineData("B1", "A2", 1)]
    [InlineData("10", "2", 1)]
    [InlineData("A1", "A1", 0)]
    public void Compare_ShouldSortPositionsNaturally(string posA, string posB, int expectedSign)
    {
        // Act
        var result = _comparer.Compare(posA, posB);

        // Assert
        Math.Sign(result).Should().Be(expectedSign);
    }

    [Fact]
    public void OrderBy_ShouldOrderShuffledNumericTracksCorrectly()
    {
        // Arrange
        var tracks = new List<Track>
        {
            Track.Create("10", "Track 10"),
            Track.Create("2", "Track 2"),
            Track.Create("1", "Track 1"),
            Track.Create("19", "Track 19"),
            Track.Create("9", "Track 9"),
            Track.Create("11", "Track 11")
        };

        // Act
        var sorted = tracks.OrderBy(t => t, _comparer).Select(t => t.Position).ToList();

        // Assert
        sorted.Should().ContainInOrder("1", "2", "9", "10", "11", "19");
    }

    [Fact]
    public void OrderBy_ShouldOrderVinylTracksCorrectly()
    {
        // Arrange
        var tracks = new List<Track>
        {
            Track.Create("B2", "Track B2"),
            Track.Create("A10", "Track A10"),
            Track.Create("A1", "Track A1"),
            Track.Create("B1", "Track B1"),
            Track.Create("A2", "Track A2"),
            Track.Create("A9", "Track A9")
        };

        // Act
        var sorted = tracks.OrderBy(t => t, _comparer).Select(t => t.Position).ToList();

        // Assert
        sorted.Should().ContainInOrder("A1", "A2", "A9", "A10", "B1", "B2");
    }

    [Fact]
    public void MediaReleaseToDto_ShouldSortTracksNaturallyRegardlessOfStorageOrder()
    {
        // Arrange
        var release = MediaRelease.CreateCd(UserId.New(), "Greatest Hits", "Artist");
        release.EnrichFromDiscogs(
            discogsReleaseId: 1234,
            tracks:
            [
                Track.Create("10", "Song Ten"),
                Track.Create("2", "Song Two"),
                Track.Create("1", "Song One"),
                Track.Create("1.2", "Song One Alternate")
            ]);

        // Act
        var dto = release.ToDto();

        // Assert
        dto.Tracks.Select(t => t.Position).Should().ContainInOrder("1", "1.2", "2", "10");
    }
}
