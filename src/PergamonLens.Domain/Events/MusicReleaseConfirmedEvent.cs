using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando el coleccionista valida y custodia un lanzamiento musical en su colección personal.
/// </summary>
public sealed record MusicReleaseConfirmedEvent(
    MusicReleaseId MusicReleaseId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MusicReleaseConfirmedEvent(MusicReleaseId musicReleaseId)
        : this(musicReleaseId, DateTime.UtcNow)
    {
    }
}

