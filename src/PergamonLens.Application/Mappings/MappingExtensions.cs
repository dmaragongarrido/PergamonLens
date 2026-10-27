using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.Mappings;

/// <summary>
/// Mapeos estáticos de alto rendimiento entre las entidades de dominio y los DTOs de aplicación.
/// </summary>
public static class MappingExtensions
{
    public static TrackDto ToDto(this Track track)
    {
        string? formattedDuration = null;
        if (track.Duration.HasValue)
        {
            var d = track.Duration.Value;
            formattedDuration = d.TotalHours >= 1
                ? d.ToString(@"hh\:mm\:ss")
                : d.ToString(@"mm\:ss");
        }

        return new TrackDto(
            track.Id,
            track.Position,
            track.Title,
            track.Duration,
            formattedDuration);
    }

    public static MusicReleaseDto ToDto(this MusicRelease release)
    {
        return new MusicReleaseDto(
            release.Id.Value,
            release.UserId.Value,
            release.Format.ToString(),
            release.Title,
            release.Artist,
            release.Label,
            release.ReleaseYear?.Value,
            release.Barcode?.Value,
            release.CatalogueNumber?.Value,
            release.Country,
            release.CoverImageUrl,
            release.DiscsCount,
            release.VinylSize?.ToString(),
            release.VinylSpeed?.ToString(),
            release.MediaCondition?.ToString(),
            release.SleeveCondition?.ToString(),
            release.Status.ToString(),
            release.DiscogsReleaseId,
            release.Notes,
            release.CreatedAtUtc,
            release.UpdatedAtUtc,
            release.Tracks.Select(t => t.ToDto()).ToList());
    }

    public static CollectionStatisticsDto ToDto(this CollectionStatistics stats)
    {
        return new CollectionStatisticsDto(
            stats.TotalReleases,
            stats.ConfirmedReleases,
            stats.PendingAnalysisReleases,
            stats.EnrichedReleases,
            stats.TotalTracks,
            stats.TotalEstimatedDuration,
            stats.FormatBreakdown.ToDictionary(k => k.Key.ToString(), v => v.Value),
            stats.ConditionBreakdown.ToDictionary(k => k.Key.ToString(), v => v.Value),
            stats.VinylSizeBreakdown.ToDictionary(k => k.Key.ToString(), v => v.Value));
    }

    public static DiscogsSearchResultDto ToDto(this DiscogsSearchResult result)
    {
        return new DiscogsSearchResultDto(
            result.ReleaseId,
            result.Title,
            result.Artist,
            result.Year,
            result.Label,
            result.CatalogueNumber,
            result.Country,
            result.CoverThumbUrl);
    }

    public static VisionAnalysisResultDto ToDto(this VisionExtractionResult result)
    {
        return new VisionAnalysisResultDto(
            result.DetectedTitle,
            result.DetectedArtist,
            result.DetectedBarcode,
            result.DetectedCatalogueNumber,
            result.DetectedLabel,
            result.Confidence,
            result.RawTextDetected,
            result.Category.ToString(),
            result.DetectedFormat,
            result.DetectedPlatform,
            result.DetectedReleaseYear,
            result.SuggestedSearchQuery);
    }
}
