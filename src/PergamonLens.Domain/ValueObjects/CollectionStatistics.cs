using PergamonLens.Domain.Enums;

namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Estadísticas agregadas y métricas globales de la colección física custodiada.
/// </summary>
public sealed record CollectionStatistics(
    int TotalReleases,
    int ConfirmedReleases,
    int PendingAnalysisReleases,
    int EnrichedReleases,
    int TotalTracks,
    TimeSpan TotalEstimatedDuration,
    IReadOnlyDictionary<MediaFormat, int> FormatBreakdown,
    IReadOnlyDictionary<MediaCondition, int> ConditionBreakdown,
    IReadOnlyDictionary<VinylSize, int> VinylSizeBreakdown)
{
    public static CollectionStatistics Empty => new(
        0,
        0,
        0,
        0,
        0,
        TimeSpan.Zero,
        new Dictionary<MediaFormat, int>(),
        new Dictionary<MediaCondition, int>(),
        new Dictionary<VinylSize, int>());
}
