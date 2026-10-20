using FluentAssertions;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.UnitTests.Application.Mappings;

public class MappingTests
{
    [Fact]
    public void MusicRelease_ToDto_ShouldMapAllFieldsAccurately()
    {
        // Arrange
        var barcode = Barcode.Create("4006381333931");
        var catNum = CatalogueNumber.Create("PCS 7027");
        var year = ReleaseYear.Create(1967);

        var release = MusicRelease.Create(
            "Sgt. Pepper's Lonely Hearts Club Band",
            "The Beatles",
            MediaFormat.Vinyl,
            barcode,
            catNum,
            year,
            discsCount: 2,
            vinylSize: VinylSize.Inch12,
            vinylSpeed: VinylSpeed.Rpm33,
            label: "Parlophone",
            country: "UK",
            coverImageUrl: "https://example.com/sgtpepper.jpg",
            notes: "Original UK pressing");

        release.AddTrack(Track.Create("A1", "Sgt. Pepper's Lonely Hearts Club Band", TimeSpan.FromSeconds(122)));
        release.AddTrack(Track.Create("A2", "With a Little Help from My Friends", TimeSpan.FromSeconds(164)));
        release.SetGrading(MediaCondition.NearMint, MediaCondition.VeryGoodPlus);

        // Act
        var dto = release.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(release.Id.Value);
        dto.Title.Should().Be("Sgt. Pepper's Lonely Hearts Club Band");
        dto.Artist.Should().Be("The Beatles");
        dto.Format.Should().Be("Vinyl");
        dto.Barcode.Should().Be("4006381333931");
        dto.CatalogueNumber.Should().Be("PCS 7027");
        dto.ReleaseYear.Should().Be(1967);
        dto.DiscsCount.Should().Be(2);
        dto.VinylSize.Should().Be("Inch12");
        dto.VinylSpeed.Should().Be("Rpm33");
        dto.Label.Should().Be("Parlophone");
        dto.Country.Should().Be("UK");
        dto.CoverImageUrl.Should().Be("https://example.com/sgtpepper.jpg");
        dto.Notes.Should().Be("Original UK pressing");
        dto.MediaCondition.Should().Be("NearMint");
        dto.SleeveCondition.Should().Be("VeryGoodPlus");
        dto.Tracks.Should().HaveCount(2);

        var track1 = dto.Tracks[0];
        track1.Position.Should().Be("A1");
        track1.Title.Should().Be("Sgt. Pepper's Lonely Hearts Club Band");
        track1.FormattedDuration.Should().Be("02:02");
    }

    [Fact]
    public void CollectionStatistics_ToDto_ShouldMapBreakdownsCorrectly()
    {
        // Arrange
        var formatBreakdown = new Dictionary<MediaFormat, int>
        {
            [MediaFormat.Vinyl] = 10,
            [MediaFormat.CD] = 5
        };
        var conditionBreakdown = new Dictionary<MediaCondition, int>
        {
            [MediaCondition.Mint] = 8,
            [MediaCondition.NearMint] = 7
        };
        var sizeBreakdown = new Dictionary<VinylSize, int>
        {
            [VinylSize.Inch12] = 10
        };

        var stats = new CollectionStatistics(
            TotalReleases: 15,
            ConfirmedReleases: 12,
            PendingAnalysisReleases: 2,
            EnrichedReleases: 1,
            TotalTracks: 150,
            TotalEstimatedDuration: TimeSpan.FromHours(10),
            FormatBreakdown: formatBreakdown,
            ConditionBreakdown: conditionBreakdown,
            VinylSizeBreakdown: sizeBreakdown);

        // Act
        var dto = stats.ToDto();

        // Assert
        dto.TotalReleases.Should().Be(15);
        dto.ConfirmedReleases.Should().Be(12);
        dto.PendingAnalysisReleases.Should().Be(2);
        dto.EnrichedReleases.Should().Be(1);
        dto.TotalTracks.Should().Be(150);
        dto.FormatBreakdown.Should().ContainKey("Vinyl").WhoseValue.Should().Be(10);
        dto.FormatBreakdown.Should().ContainKey("CD").WhoseValue.Should().Be(5);
        dto.ConditionBreakdown.Should().ContainKey("Mint").WhoseValue.Should().Be(8);
        dto.VinylSizeBreakdown.Should().ContainKey("Inch12").WhoseValue.Should().Be(10);
    }

    [Fact]
    public void DiscogsSearchResult_ToDto_ShouldMapProperties()
    {
        // Arrange
        var result = new DiscogsSearchResult(
            ReleaseId: 123456,
            Title: "Kind of Blue",
            Artist: "Miles Davis",
            Year: 1959,
            Label: "Columbia",
            CatalogueNumber: "CL 1355",
            Country: "US",
            CoverThumbUrl: "https://example.com/thumb.jpg");

        // Act
        var dto = result.ToDto();

        // Assert
        dto.ReleaseId.Should().Be(123456);
        dto.Title.Should().Be("Kind of Blue");
        dto.Artist.Should().Be("Miles Davis");
        dto.Year.Should().Be(1959);
        dto.Label.Should().Be("Columbia");
        dto.CatalogueNumber.Should().Be("CL 1355");
        dto.Country.Should().Be("US");
        dto.CoverThumbUrl.Should().Be("https://example.com/thumb.jpg");
    }

    [Fact]
    public void VisionExtractionResult_ToDto_ShouldMapProperties()
    {
        // Arrange
        var result = new VisionExtractionResult(
            DetectedTitle: "Abbey Road",
            DetectedArtist: "The Beatles",
            DetectedBarcode: "0094638246817",
            DetectedCatalogueNumber: "PCS 7088",
            DetectedLabel: "Apple Records",
            Confidence: 0.96,
            RawTextDetected: "THE BEATLES ABBEY ROAD PCS 7088");

        // Act
        var dto = result.ToDto();

        // Assert
        dto.DetectedTitle.Should().Be("Abbey Road");
        dto.DetectedArtist.Should().Be("The Beatles");
        dto.DetectedBarcode.Should().Be("0094638246817");
        dto.DetectedCatalogueNumber.Should().Be("PCS 7088");
        dto.Confidence.Should().Be(0.96);
        dto.RawTextDetected.Should().Contain("ABBEY ROAD");
    }
}

