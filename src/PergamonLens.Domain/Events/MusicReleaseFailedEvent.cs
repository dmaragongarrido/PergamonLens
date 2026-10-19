using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando el procesamiento de reconocimiento o enriquecimiento falla.
/// </summary>
public sealed record MusicReleaseFailedEvent(
    MusicReleaseId MusicReleaseId,
    string Reason,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MusicReleaseFailedEvent(MusicReleaseId musicReleaseId, string reason)
        : this(musicReleaseId, reason, DateTime.UtcNow)
    {
    }
}

