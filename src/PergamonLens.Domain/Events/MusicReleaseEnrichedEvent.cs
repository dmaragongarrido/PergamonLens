using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando un lanzamiento musical ha sido contrastado y enriquecido con Discogs.
/// </summary>
public sealed record MusicReleaseEnrichedEvent(
    MusicReleaseId MusicReleaseId,
    long DiscogsReleaseId,
    int TracksCount,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MusicReleaseEnrichedEvent(MusicReleaseId musicReleaseId, long discogsReleaseId, int tracksCount)
        : this(musicReleaseId, discogsReleaseId, tracksCount, DateTime.UtcNow)
    {
    }
}

