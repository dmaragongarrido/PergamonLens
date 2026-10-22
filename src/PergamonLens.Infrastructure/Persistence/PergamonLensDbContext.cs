using Microsoft.EntityFrameworkCore;
using PergamonLens.Domain.Entities;

namespace PergamonLens.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de Entity Framework Core para la persistencia en SQL Server.
/// </summary>
public sealed class PergamonLensDbContext : DbContext
{
    public DbSet<MusicRelease> MusicReleases => Set<MusicRelease>();
    public DbSet<Track> Tracks => Set<Track>();

    public PergamonLensDbContext(DbContextOptions<PergamonLensDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PergamonLensDbContext).Assembly);
    }
}
