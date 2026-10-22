using FluentAssertions;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Events;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;
using Xunit;

namespace PergamonLens.UnitTests.Entities;

public class MusicReleaseTests
{
    private readonly UserId _testUserId = UserId.New();

    [Fact]
    public void CreateVinyl_WithValidData_ShouldInitializeCorrectlyAndRaiseCreatedEvent()
    {
        // Arrange
        const string title = "The Dark Side of the Moon";
        const string artist = "Pink Floyd";
        var barcode = Barcode.Create("5099902987613");
        var catalogueNumber = CatalogueNumber.Create("SHVL 804");
        var releaseYear = ReleaseYear.Create(1973);

        // Act
        var release = MusicRelease.CreateVinyl(
            _testUserId,
            title,
            artist,
            VinylSize.Inch12,
            VinylSpeed.Rpm33,
            discsCount: 1,
            barcode: barcode,
            catalogueNumber: catalogueNumber,
            releaseYear: releaseYear,
            label: "Harvest",
            country: "UK");

        // Assert
        release.Id.Value.Should().NotBeEmpty();
        release.UserId.Should().Be(_testUserId);
        release.Format.Should().Be(MediaFormat.Vinyl);
        release.Title.Should().Be(title);
        release.Artist.Should().Be(artist);
        release.Barcode.Should().Be(barcode);
        release.CatalogueNumber.Should().Be(catalogueNumber);
        release.ReleaseYear.Should().Be(releaseYear);
        release.VinylSize.Should().Be(VinylSize.Inch12);
        release.VinylSpeed.Should().Be(VinylSpeed.Rpm33);
        release.DiscsCount.Should().Be(1);
        release.Label.Should().Be("Harvest");
        release.Country.Should().Be("UK");
        release.Status.Should().Be(ProcessingStatus.PendingAnalysis);
        release.Tracks.Should().BeEmpty();

        release.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MusicReleaseCreatedEvent>()
            .Which.MusicReleaseId.Should().Be(release.Id);
    }

    [Fact]
    public void CreateCd_WithValidData_ShouldSetFormatToCdAndNullVinylSpecs()
    {
        // Arrange & Act
        var cd = MusicRelease.CreateCd(
            _testUserId,
            "OK Computer",
            "Radiohead",
            discsCount: 1,
            label: "Parlophone",
            country: "UK");

        // Assert
        cd.UserId.Should().Be(_testUserId);
        cd.Format.Should().Be(MediaFormat.CD);
        cd.VinylSize.Should().BeNull();
        cd.VinylSpeed.Should().BeNull();
        cd.DiscsCount.Should().Be(1);
        cd.Title.Should().Be("OK Computer");
        cd.Artist.Should().Be("Radiohead");
        cd.Status.Should().Be(ProcessingStatus.PendingAnalysis);
    }

    [Fact]
    public void CreateCassette_WithValidData_ShouldSetFormatToCassette()
    {
        // Arrange & Act
        var tape = MusicRelease.CreateCassette(
            _testUserId,
            "Master of Puppets",
            "Metallica",
            tapesCount: 1,
            label: "Elektra");

        // Assert
        tape.UserId.Should().Be(_testUserId);
        tape.Format.Should().Be(MediaFormat.Cassette);
        tape.VinylSize.Should().BeNull();
        tape.VinylSpeed.Should().BeNull();
        tape.DiscsCount.Should().Be(1);
        tape.Title.Should().Be("Master of Puppets");
        tape.Artist.Should().Be("Metallica");
    }

    [Fact]
    public void Create_WithNullUserId_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => MusicRelease.Create(null!, "Title", "Artist");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ShouldThrowMusicReleaseDomainException(string? invalidTitle)
    {
        // Act
        var act = () => MusicRelease.Create(_testUserId, invalidTitle!, "Pink Floyd");

        // Assert
        act.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*título*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidArtist_ShouldThrowMusicReleaseDomainException(string? invalidArtist)
    {
        // Act
        var act = () => MusicRelease.Create(_testUserId, "The Dark Side of the Moon", invalidArtist!);

        // Assert
        act.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*artista*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidDiscsCount_ShouldThrowMusicReleaseDomainException(int invalidDiscsCount)
    {
        // Act
        var act = () => MusicRelease.Create(_testUserId, "Title", "Artist", discsCount: invalidDiscsCount);

        // Assert
        act.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*discos*");
    }

    [Fact]
    public void SetVinylSpecifications_OnNonVinylFormat_ShouldThrowMusicReleaseDomainException()
    {
        // Arrange
        var cd = MusicRelease.CreateCd(_testUserId, "Discovery", "Daft Punk");

        // Act
        var act = () => cd.SetVinylSpecifications(VinylSize.Inch12, VinylSpeed.Rpm33);

        // Assert
        act.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*Vinyl*");
    }

    [Fact]
    public void SetVinylSpecifications_OnVinylFormat_ShouldUpdateCorrectly()
    {
        // Arrange
        var vinyl = MusicRelease.CreateVinyl(_testUserId, "Blue Train", "John Coltrane");

        // Act
        vinyl.SetVinylSpecifications(VinylSize.Inch10, VinylSpeed.Rpm45, discsCount: 2);

        // Assert
        vinyl.VinylSize.Should().Be(VinylSize.Inch10);
        vinyl.VinylSpeed.Should().Be(VinylSpeed.Rpm45);
        vinyl.DiscsCount.Should().Be(2);
        vinyl.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void UpdateIdentifiedDetails_WhenPendingAnalysis_ShouldUpdateValuesAndTransitionToIdentified()
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "Unknown Title", "Unknown Artist");
        var barcode = Barcode.Create("012345678905");
        var catNo = CatalogueNumber.Create("CAT-001");
        var year = ReleaseYear.Create(1982);

        // Act
        release.UpdateIdentifiedDetails(
            "Thriller",
            "Michael Jackson",
            barcode,
            catNo,
            year,
            "Epic",
            "US");

        // Assert
        release.Title.Should().Be("Thriller");
        release.Artist.Should().Be("Michael Jackson");
        release.Barcode.Should().Be(barcode);
        release.CatalogueNumber.Should().Be(catNo);
        release.ReleaseYear.Should().Be(year);
        release.Label.Should().Be("Epic");
        release.Country.Should().Be("US");
        release.Status.Should().Be(ProcessingStatus.Identified);
        release.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void EnrichFromDiscogs_WithValidData_ShouldUpdateMetadataTracksAndRaiseEnrichedEvent()
    {
        // Arrange
        var release = MusicRelease.CreateCd(_testUserId, "Abbey Road", "The Beatles");
        var tracks = new List<Track>
        {
            Track.Create("1", "Come Together", TimeSpan.FromSeconds(259)),
            Track.Create("2", "Something", TimeSpan.FromSeconds(182))
        };

        // Act
        release.EnrichFromDiscogs(
            discogsReleaseId: 24395,
            label: "Apple Records",
            releaseYear: ReleaseYear.Create(1969),
            country: "UK",
            coverImageUrl: "https://discogs.com/img/abbey-road.jpg",
            tracks: tracks);

        // Assert
        release.Status.Should().Be(ProcessingStatus.Enriched);
        release.DiscogsReleaseId.Should().Be(24395);
        release.Label.Should().Be("Apple Records");
        release.Country.Should().Be("UK");
        release.CoverImageUrl.Should().Be("https://discogs.com/img/abbey-road.jpg");
        release.Tracks.Should().HaveCount(2);

        release.DomainEvents.Should().Contain(e => e is MusicReleaseEnrichedEvent)
            .Which.Should().BeOfType<MusicReleaseEnrichedEvent>()
            .Which.DiscogsReleaseId.Should().Be(24395);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void EnrichFromDiscogs_WithInvalidReleaseId_ShouldThrowMusicReleaseDomainException(long invalidReleaseId)
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "Abbey Road", "The Beatles");

        // Act
        var act = () => release.EnrichFromDiscogs(invalidReleaseId);

        // Assert
        act.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*Discogs*");
    }

    [Fact]
    public void Confirm_WhenNotInFailedState_ShouldTransitionToConfirmedAndRaiseConfirmedEvent()
    {
        // Arrange
        var release = MusicRelease.CreateVinyl(_testUserId, "Kind of Blue", "Miles Davis");

        // Act
        release.Confirm();

        // Assert
        release.Status.Should().Be(ProcessingStatus.Confirmed);
        release.UpdatedAtUtc.Should().NotBeNull();

        release.DomainEvents.Should().Contain(e => e is MusicReleaseConfirmedEvent);
    }

    [Fact]
    public void Confirm_WhenInFailedState_ShouldThrowMusicReleaseDomainException()
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "Unknown", "Unknown");
        release.MarkAsFailed("Imagen borrosa");

        // Act
        var act = () => release.Confirm();

        // Assert
        act.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*fallido*");
    }

    [Fact]
    public void Operations_WhenConfirmed_ShouldThrowMusicReleaseDomainException()
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "A Love Supreme", "John Coltrane");
        release.Confirm();

        // Act & Assert
        var actUpdate = () => release.UpdateIdentifiedDetails("New Title", "New Artist");
        actUpdate.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*confirmado*");

        var actEnrich = () => release.EnrichFromDiscogs(12345);
        actEnrich.Should().Throw<MusicReleaseDomainException>()
            .WithMessage("*confirmado*");
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusToFailedAndRaiseFailedEvent()
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "Blurry Cover", "Unknown");

        // Act
        release.MarkAsFailed("Código de barras ilegible");

        // Assert
        release.Status.Should().Be(ProcessingStatus.Failed);
        release.Notes.Should().Contain("ilegible");

        release.DomainEvents.Should().Contain(e => e is MusicReleaseFailedEvent)
            .Which.Should().BeOfType<MusicReleaseFailedEvent>()
            .Which.Reason.Should().Be("Código de barras ilegible");
    }

    [Fact]
    public void AddTrack_WhenPositionAlreadyExists_ShouldThrowInvalidTrackException()
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "A Night at the Opera", "Queen");
        release.AddTrack(Track.Create("A1", "Death on Two Legs"));

        // Act
        var act = () => release.AddTrack(Track.Create("A1", "Lazing on a Sunday Afternoon"));

        // Assert
        act.Should().Throw<InvalidTrackException>()
            .WithMessage("*A1*");
    }

    [Fact]
    public void RemoveTrack_WithExistingTrackId_ShouldRemoveItFromTracksCollection()
    {
        // Arrange
        var release = MusicRelease.Create(_testUserId, "Rumours", "Fleetwood Mac");
        var track = Track.Create("A1", "Second Hand News");
        release.AddTrack(track);

        // Act
        release.RemoveTrack(track.Id);

        // Assert
        release.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void SetGrading_ShouldUpdateConditions()
    {
        // Arrange
        var release = MusicRelease.CreateVinyl(_testUserId, "Led Zeppelin IV", "Led Zeppelin");

        // Act
        release.SetGrading(MediaCondition.NearMint, MediaCondition.VeryGoodPlus);

        // Assert
        release.MediaCondition.Should().Be(MediaCondition.NearMint);
        release.SleeveCondition.Should().Be(MediaCondition.VeryGoodPlus);
        release.UpdatedAtUtc.Should().NotBeNull();
    }
}
