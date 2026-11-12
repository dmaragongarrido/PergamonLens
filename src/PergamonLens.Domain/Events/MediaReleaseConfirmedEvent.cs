using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando el coleccionista valida y custodia un ejemplar físico en su colección personal.
/// </summary>
public sealed record MediaReleaseConfirmedEvent(
    MediaReleaseId MediaReleaseId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MediaReleaseConfirmedEvent(MediaReleaseId mediaReleaseId)
        : this(mediaReleaseId, DateTime.UtcNow)
    {
    }
}
