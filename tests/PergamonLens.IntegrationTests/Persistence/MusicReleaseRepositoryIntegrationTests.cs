using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.Persistence;
using PergamonLens.Infrastructure.Persistence.Repositories;

namespace PergamonLens.IntegrationTests.Persistence;

public class MusicReleaseRepositoryIntegrationTests : IDisposable
{
    private readonly DbContextOptions<PergamonLensDbContext> _options;
    private readonly PergamonLensDbContext _context;
    private readonly MusicReleaseRepository _repository;
    private readonly CollectionRepository _collectionRepository;
    private readonly List<MusicReleaseId> _createdIds = [];
    private readonly UserId _testUserId = UserId.New();

    public MusicReleaseRepositoryIntegrationTests()
    {
        string defaultConnection = "Host=localhost;Port=5432;Database=PergamonLensDb;Username=postgres;Password=postgres";

        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? defaultConnection;

        _options = new DbContextOptionsBuilder<PergamonLensDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new PergamonLensDbContext(_options);
        _context.Database.Migrate();

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
            _testUserId,
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
        retrieved.UserId.Should().Be(_testUserId);
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
            _testUserId,
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
        var release = MusicRelease.Create(_testUserId, "To Be Deleted", "Ephemeral Artist");
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

    [Fact]
    public async Task MultiUserIsolation_WhenQueryingOtherUserCollection_ShouldEnforceStrictDataIsolation()
    {
        // Arrange: Dos usuarios distintos con sus respectivos identificadores
        var userA = UserId.New();
        var userB = UserId.New();

        var releaseUserA = MusicRelease.Create(userA, "Album User A", "Artist A", MediaFormat.Vinyl);
        var releaseUserB = MusicRelease.Create(userB, "Album User B", "Artist B", MediaFormat.CD);

        _createdIds.Add(releaseUserA.Id);
        _createdIds.Add(releaseUserB.Id);

        // Persistimos ambos usando el contexto sin filtro de sistema
        await _repository.AddAsync(releaseUserA);
        await _repository.AddAsync(releaseUserB);

        // Simulamos contexto HTTP para Usuario A
        var serviceA = new TestCurrentUserService(userA.Value);
        using var contextUserA = new PergamonLensDbContext(_options, serviceA);
        var repoUserA = new MusicReleaseRepository(contextUserA);

        // Simulamos contexto HTTP para Usuario B
        var serviceB = new TestCurrentUserService(userB.Value);
        using var contextUserB = new PergamonLensDbContext(_options, serviceB);
        var repoUserB = new MusicReleaseRepository(contextUserB);

        // Act & Assert:
        // 1. Usuario A puede ver su álbum pero NO el de Usuario B
        var foundByA = await repoUserA.GetByIdAsync(releaseUserA.Id);
        var unauthorizedByA = await repoUserA.GetByIdAsync(releaseUserB.Id);

        foundByA.Should().NotBeNull();
        foundByA!.Title.Should().Be("Album User A");
        unauthorizedByA.Should().BeNull(); // BOLA / IDOR mitigation: retorna null

        // 2. Usuario B puede ver su álbum pero NO el de Usuario A
        var foundByB = await repoUserB.GetByIdAsync(releaseUserB.Id);
        var unauthorizedByB = await repoUserB.GetByIdAsync(releaseUserA.Id);

        foundByB.Should().NotBeNull();
        foundByB!.Title.Should().Be("Album User B");
        unauthorizedByB.Should().BeNull();

        // 3. Búsqueda y colecciones aisladas
        var userAReleases = await repoUserA.GetAllAsync();
        userAReleases.Should().Contain(r => r.Id == releaseUserA.Id);
        userAReleases.Should().NotContain(r => r.Id == releaseUserB.Id);

        var userBReleases = await repoUserB.GetAllAsync();
        userBReleases.Should().Contain(r => r.Id == releaseUserB.Id);
        userBReleases.Should().NotContain(r => r.Id == releaseUserA.Id);
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

    private sealed class TestCurrentUserService(string userId) : ICurrentUserService
    {
        public string? UserId => userId;
        public bool IsAuthenticated => true;
    }
}
