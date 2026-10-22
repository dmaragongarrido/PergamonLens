using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.Persistence;
using PergamonLens.Infrastructure.Persistence.Repositories;

namespace PergamonLens.IntegrationTests.Persistence;

public class MusicReleaseRepositoryIntegrationTests : IDisposable
{
    private readonly PergamonLensDbContext _context;
    private readonly MusicReleaseRepository _repository;
    private readonly CollectionRepository _collectionRepository;
    private readonly List<MusicReleaseId> _createdIds = [];

    public MusicReleaseRepositoryIntegrationTests()
    {
        string defaultConnection = OperatingSystem.IsWindows()
            ? "Server=(localdb)\\mssqllocaldb;Database=PergamonLensDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
            : "Server=localhost,1433;Database=PergamonLensDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;MultipleActiveResultSets=true";

        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? defaultConnection;

        var options = new DbContextOptionsBuilder<PergamonLensDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new PergamonLensDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new MusicReleaseRepository(_context);
        _collectionRepository = new CollectionRepository(_context);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ShouldPersistAndRetrieveAggregateWithTracks()
    {
        // Arrange
        var barcode = Barcode.Create("4006381333931");
        var catNum = CatalogueNumber.Create("TEST-SQL-01");
        var year = ReleaseYear.Create(1975);

        var release = MusicRelease.Create(
            "Integration Test Album",
            "SQL Server Artist",
            MediaFormat.Vinyl,
            barcode,
            catNum,
            year,
            discsCount: 1,
            vinylSize: VinylSize.Inch12,
            vinylSpeed: VinylSpeed.Rpm33,
            label: "SQL Records",
            country: "ES");

        release.AddTrack(Track.Create("A1", "Track One", TimeSpan.FromSeconds(210)));
        release.AddTrack(Track.Create("A2", "Track Two", TimeSpan.FromSeconds(185)));

        _createdIds.Add(release.Id);

        // Act
        await _repository.AddAsync(release);

        // Assert
        var retrieved = await _repository.GetByIdAsync(release.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(release.Id);
        retrieved.Title.Should().Be("Integration Test Album");
        retrieved.Artist.Should().Be("SQL Server Artist");
        retrieved.Barcode.Should().NotBeNull();
        retrieved.Barcode!.Value.Should().Be("4006381333931");
        retrieved.CatalogueNumber.Should().NotBeNull();
        retrieved.CatalogueNumber!.Value.Should().Be("TEST-SQL-01");
        retrieved.Tracks.Should().HaveCount(2);
        retrieved.Tracks.Should().Contain(t => t.Position == "A1" && t.Title == "Track One");
    }

    [Fact]
    public async Task SearchAsync_ShouldFindReleaseByArtistOrTitle()
    {
        // Arrange
        var release = MusicRelease.Create(
            "Distinctive Echoes",
            "Unique Band Name",
            MediaFormat.CD);

        _createdIds.Add(release.Id);
        await _repository.AddAsync(release);

        // Act
        var results = await _repository.SearchAsync("Distinctive Echoes");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.Id == release.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveReleaseAndCascadeDeleteTracks()
    {
        // Arrange
        var release = MusicRelease.Create("To Be Deleted", "Ephemeral Artist");
        release.AddTrack(Track.Create("A1", "Doomed Track"));

        await _repository.AddAsync(release);

        // Act
        await _repository.DeleteAsync(release);

        // Assert
        var found = await _repository.GetByIdAsync(release.Id);
        found.Should().BeNull();

        var orphanTracks = await _context.Tracks
            .Where(t => EF.Property<MusicReleaseId>(t, "MusicReleaseId") == release.Id)
            .ToListAsync();

        orphanTracks.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (_createdIds.Count > 0)
        {
            var releasesToDelete = _context.MusicReleases
                .Where(r => _createdIds.Contains(r.Id))
                .ToList();

            if (releasesToDelete.Count > 0)
            {
                _context.MusicReleases.RemoveRange(releasesToDelete);
                _context.SaveChanges();
            }
        }

        _context.Dispose();
    }
}
