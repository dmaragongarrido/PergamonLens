using PergamonLens.Domain.Common;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido al registrar o escanear un nuevo lanzamiento musical en el sistema.
/// </summary>
public sealed record MusicReleaseCreatedEvent(
    MusicReleaseId MusicReleaseId,
    string Title,
    string Artist,
    MediaFormat Format,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MusicReleaseCreatedEvent(MusicReleaseId musicReleaseId, string title, string artist, MediaFormat format)
        : this(musicReleaseId, title, artist, format, DateTime.UtcNow)
    {
    }
}

