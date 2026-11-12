using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando un ejemplar físico ha sido contrastado y enriquecido con catálogo externo.
/// </summary>
public sealed record MediaReleaseEnrichedEvent(
    MediaReleaseId MediaReleaseId,
    long DiscogsReleaseId,
    int TracksCount,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MediaReleaseEnrichedEvent(MediaReleaseId mediaReleaseId, long discogsReleaseId, int tracksCount)
        : this(mediaReleaseId, discogsReleaseId, tracksCount, DateTime.UtcNow)
    {
    }
}
