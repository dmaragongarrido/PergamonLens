namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO con métricas y estadísticas agregadas de la colección de medios físicos.
/// </summary>
public sealed record CollectionStatisticsDto(
    int TotalReleases,
    int ConfirmedReleases,
    int PendingAnalysisReleases,
    int EnrichedReleases,
    int TotalTracks,
    TimeSpan TotalEstimatedDuration,
    IReadOnlyDictionary<string, int> FormatBreakdown,
    IReadOnlyDictionary<string, int> ConditionBreakdown,
    IReadOnlyDictionary<string, int> VinylSizeBreakdown);

