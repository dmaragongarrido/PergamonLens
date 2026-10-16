using FluentAssertions;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Events;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;
using Xunit;

namespace PergamonLens.UnitTests.Entities;

public class VinylRecordTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectlyAndRaiseCreatedEvent()
    {
        // Arrange
        const string title = "The Dark Side of the Moon";
        const string artist = "Pink Floyd";
        var barcode = Barcode.Create("5099902987613");
        var catalogueNumber = CatalogueNumber.Create("SHVL 804");
        var releaseYear = ReleaseYear.Create(1973);

        // Act
        var vinyl = VinylRecord.Create(
            title,
            artist,
            barcode,
            catalogueNumber,
            releaseYear,
            VinylSize.Inch12,
            VinylSpeed.Rpm33,
            discsCount: 1,
            label: "Harvest",
            country: "UK");

        // Assert
        vinyl.Id.Value.Should().NotBeEmpty();
        vinyl.Title.Should().Be(title);
        vinyl.Artist.Should().Be(artist);
        vinyl.Barcode.Should().Be(barcode);
        vinyl.CatalogueNumber.Should().Be(catalogueNumber);
        vinyl.ReleaseYear.Should().Be(releaseYear);
        vinyl.Size.Should().Be(VinylSize.Inch12);
        vinyl.Speed.Should().Be(VinylSpeed.Rpm33);
        vinyl.DiscsCount.Should().Be(1);
        vinyl.Label.Should().Be("Harvest");
        vinyl.Country.Should().Be("UK");
        vinyl.Status.Should().Be(ProcessingStatus.PendingAnalysis);
        vinyl.Tracks.Should().BeEmpty();

        vinyl.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<VinylRecordCreatedEvent>()
            .Which.VinylRecordId.Should().Be(vinyl.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ShouldThrowVinylRecordDomainException(string? invalidTitle)
    {
        // Act
        var act = () => VinylRecord.Create(invalidTitle!, "Pink Floyd");

        // Assert
        act.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*título*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidArtist_ShouldThrowVinylRecordDomainException(string? invalidArtist)
    {
        // Act
        var act = () => VinylRecord.Create("The Dark Side of the Moon", invalidArtist!);

        // Assert
        act.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*artista*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidDiscsCount_ShouldThrowVinylRecordDomainException(int invalidDiscsCount)
    {
        // Act
        var act = () => VinylRecord.Create("Title", "Artist", discsCount: invalidDiscsCount);

        // Assert
        act.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*discos*");
    }

    [Fact]
    public void UpdateIdentifiedDetails_WhenPendingAnalysis_ShouldUpdateValuesAndTransitionToIdentified()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Unknown Title", "Unknown Artist");
        var barcode = Barcode.Create("012345678905");
        var catNo = CatalogueNumber.Create("CAT-001");
        var year = ReleaseYear.Create(1982);

        // Act
        vinyl.UpdateIdentifiedDetails(
            "Thriller",
            "Michael Jackson",
            barcode,
            catNo,
            year,
            "Epic",
            "US");

        // Assert
        vinyl.Title.Should().Be("Thriller");
        vinyl.Artist.Should().Be("Michael Jackson");
        vinyl.Barcode.Should().Be(barcode);
        vinyl.CatalogueNumber.Should().Be(catNo);
        vinyl.ReleaseYear.Should().Be(year);
        vinyl.Label.Should().Be("Epic");
        vinyl.Country.Should().Be("US");
        vinyl.Status.Should().Be(ProcessingStatus.Identified);
        vinyl.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void EnrichFromDiscogs_WithValidData_ShouldUpdateMetadataTracksAndRaiseEnrichedEvent()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Abbey Road", "The Beatles");
        var tracks = new List<Track>
        {
            Track.Create("A1", "Come Together", TimeSpan.FromSeconds(259)),
            Track.Create("A2", "Something", TimeSpan.FromSeconds(182))
        };

        // Act
        vinyl.EnrichFromDiscogs(
            discogsReleaseId: 24395,
            label: "Apple Records",
            releaseYear: ReleaseYear.Create(1969),
            country: "UK",
            coverImageUrl: "https://discogs.com/img/abbey-road.jpg",
            tracks: tracks);

        // Assert
        vinyl.Status.Should().Be(ProcessingStatus.Enriched);
        vinyl.DiscogsReleaseId.Should().Be(24395);
        vinyl.Label.Should().Be("Apple Records");
        vinyl.Country.Should().Be("UK");
        vinyl.CoverImageUrl.Should().Be("https://discogs.com/img/abbey-road.jpg");
        vinyl.Tracks.Should().HaveCount(2);

        vinyl.DomainEvents.Should().Contain(e => e is VinylRecordEnrichedEvent)
            .Which.Should().BeOfType<VinylRecordEnrichedEvent>()
            .Which.DiscogsReleaseId.Should().Be(24395);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void EnrichFromDiscogs_WithInvalidReleaseId_ShouldThrowVinylRecordDomainException(long invalidReleaseId)
    {
        // Arrange
        var vinyl = VinylRecord.Create("Abbey Road", "The Beatles");

        // Act
        var act = () => vinyl.EnrichFromDiscogs(invalidReleaseId);

        // Assert
        act.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*Discogs*");
    }

    [Fact]
    public void Confirm_WhenNotInFailedState_ShouldTransitionToConfirmedAndRaiseConfirmedEvent()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Kind of Blue", "Miles Davis");

        // Act
        vinyl.Confirm();

        // Assert
        vinyl.Status.Should().Be(ProcessingStatus.Confirmed);
        vinyl.UpdatedAtUtc.Should().NotBeNull();

        vinyl.DomainEvents.Should().Contain(e => e is VinylRecordConfirmedEvent);
    }

    [Fact]
    public void Confirm_WhenInFailedState_ShouldThrowVinylRecordDomainException()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Unknown", "Unknown");
        vinyl.MarkAsFailed("Imagen borrosa");

        // Act
        var act = () => vinyl.Confirm();

        // Assert
        act.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*fallido*");
    }

    [Fact]
    public void Operations_WhenConfirmed_ShouldThrowVinylRecordDomainException()
    {
        // Arrange
        var vinyl = VinylRecord.Create("A Love Supreme", "John Coltrane");
        vinyl.Confirm();

        // Act & Assert
        var actUpdate = () => vinyl.UpdateIdentifiedDetails("New Title", "New Artist");
        actUpdate.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*confirmado*");

        var actEnrich = () => vinyl.EnrichFromDiscogs(12345);
        actEnrich.Should().Throw<VinylRecordDomainException>()
            .WithMessage("*confirmado*");
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusToFailedAndRaiseFailedEvent()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Blurry Cover", "Unknown");

        // Act
        vinyl.MarkAsFailed("Código de barras ilegible");

        // Assert
        vinyl.Status.Should().Be(ProcessingStatus.Failed);
        vinyl.Notes.Should().Contain("ilegible");

        vinyl.DomainEvents.Should().Contain(e => e is VinylRecordFailedEvent)
            .Which.Should().BeOfType<VinylRecordFailedEvent>()
            .Which.Reason.Should().Be("Código de barras ilegible");
    }

    [Fact]
    public void AddTrack_WhenPositionAlreadyExists_ShouldThrowInvalidTrackException()
    {
        // Arrange
        var vinyl = VinylRecord.Create("A Night at the Opera", "Queen");
        vinyl.AddTrack(Track.Create("A1", "Death on Two Legs"));

        // Act
        var act = () => vinyl.AddTrack(Track.Create("A1", "Lazing on a Sunday Afternoon"));

        // Assert
        act.Should().Throw<InvalidTrackException>()
            .WithMessage("*A1*");
    }

    [Fact]
    public void RemoveTrack_WithExistingTrackId_ShouldRemoveItFromTracksCollection()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Rumours", "Fleetwood Mac");
        var track = Track.Create("A1", "Second Hand News");
        vinyl.AddTrack(track);

        // Act
        vinyl.RemoveTrack(track.Id);

        // Assert
        vinyl.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void SetGrading_ShouldUpdateConditions()
    {
        // Arrange
        var vinyl = VinylRecord.Create("Led Zeppelin IV", "Led Zeppelin");

        // Act
        vinyl.SetGrading(MediaCondition.NearMint, MediaCondition.VeryGoodPlus);

        // Assert
        vinyl.MediaCondition.Should().Be(MediaCondition.NearMint);
        vinyl.SleeveCondition.Should().Be(MediaCondition.VeryGoodPlus);
        vinyl.UpdatedAtUtc.Should().NotBeNull();
    }
}
