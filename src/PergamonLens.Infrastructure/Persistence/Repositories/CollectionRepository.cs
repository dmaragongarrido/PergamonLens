using Microsoft.EntityFrameworkCore;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación con Entity Framework Core del repositorio de consultas agregadas sobre la colección (<see cref="ICollectionRepository"/>).
/// </summary>
public sealed class CollectionRepository : ICollectionRepository
{
    private readonly PergamonLensDbContext _context;

    public CollectionRepository(PergamonLensDbContext context)
    {
        _context = context;
    }

    public async Task<CollectionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalReleases = await _context.MusicReleases.CountAsync(cancellationToken);
        var confirmedReleases = await _context.MusicReleases.CountAsync(r => r.Status == ProcessingStatus.Confirmed, cancellationToken);
        var pendingAnalysisReleases = await _context.MusicReleases.CountAsync(r => r.Status == ProcessingStatus.PendingAnalysis, cancellationToken);
        var enrichedReleases = await _context.MusicReleases.CountAsync(r => r.Status == ProcessingStatus.Enriched, cancellationToken);

        var totalTracks = await _context.Tracks.CountAsync(cancellationToken);

        // Duración estimada total acumulada
        var durations = await _context.Tracks
            .Where(t => t.Duration != null)
            .Select(t => t.Duration!.Value)
            .ToListAsync(cancellationToken);

        var totalEstimatedDuration = durations.Aggregate(TimeSpan.Zero, (acc, next) => acc + next);

        // Desglose por formato
        var formatGroups = await _context.MusicReleases
            .GroupBy(r => r.Format)
            .Select(g => new { Format = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var formatBreakdown = formatGroups.ToDictionary(g => g.Format, g => g.Count);

        // Desglose por condición física
        var conditionGroups = await _context.MusicReleases
            .Where(r => r.MediaCondition != null)
            .GroupBy(r => r.MediaCondition!.Value)
            .Select(g => new { Condition = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var conditionBreakdown = conditionGroups.ToDictionary(g => g.Condition, g => g.Count);

        // Desglose por tamaño de vinilo
        var sizeGroups = await _context.MusicReleases
            .Where(r => r.VinylSize != null)
            .GroupBy(r => r.VinylSize!.Value)
            .Select(g => new { Size = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vinylSizeBreakdown = sizeGroups.ToDictionary(g => g.Size, g => g.Count);

        return new CollectionStatistics(
            TotalReleases: totalReleases,
            ConfirmedReleases: confirmedReleases,
            PendingAnalysisReleases: pendingAnalysisReleases,
            EnrichedReleases: enrichedReleases,
            TotalTracks: totalTracks,
            TotalEstimatedDuration: totalEstimatedDuration,
            FormatBreakdown: formatBreakdown,
            ConditionBreakdown: conditionBreakdown,
            VinylSizeBreakdown: vinylSizeBreakdown);
    }

    public async Task<IReadOnlyList<MusicRelease>> GetRecentAdditionsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MusicRelease>> GetByArtistAsync(string artist, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .Where(r => r.Artist.Contains(artist))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MusicRelease>> GetByFormatAsync(MediaFormat format, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .Where(r => r.Format == format)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
