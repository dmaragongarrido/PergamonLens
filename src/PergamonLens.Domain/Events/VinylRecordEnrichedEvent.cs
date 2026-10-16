using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando un vinilo ha sido contrastado y enriquecido con Discogs.
/// </summary>
public sealed record VinylRecordEnrichedEvent(
    VinylRecordId VinylRecordId,
    long DiscogsReleaseId,
    int TracksCount,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public VinylRecordEnrichedEvent(VinylRecordId vinylRecordId, long discogsReleaseId, int tracksCount)
        : this(vinylRecordId, discogsReleaseId, tracksCount, DateTime.UtcNow)
    {
    }
}
