using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando el procesamiento de reconocimiento o enriquecimiento de un ejemplar físico falla.
/// </summary>
public sealed record MediaReleaseFailedEvent(
    MediaReleaseId MediaReleaseId,
    string Reason,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MediaReleaseFailedEvent(MediaReleaseId mediaReleaseId, string reason)
        : this(mediaReleaseId, reason, DateTime.UtcNow)
    {
    }
}
