using PergamonLens.Domain.Enums;

namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Estadísticas agregadas y métricas globales de la colección de vinilos custodiada.
/// </summary>
public sealed record CollectionStatistics(
    int TotalRecords,
    int ConfirmedRecords,
    int PendingAnalysisRecords,
    int EnrichedRecords,
    int TotalTracks,
    TimeSpan TotalEstimatedDuration,
    IReadOnlyDictionary<MediaCondition, int> ConditionBreakdown,
    IReadOnlyDictionary<VinylSize, int> SizeBreakdown)
{
    public static CollectionStatistics Empty => new(
        0,
        0,
        0,
        0,
        0,
        TimeSpan.Zero,
        new Dictionary<MediaCondition, int>(),
        new Dictionary<VinylSize, int>());
}
