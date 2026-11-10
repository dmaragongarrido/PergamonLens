using PergamonLens.Domain.Common;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido al registrar o escanear un nuevo ejemplar físico en el sistema.
/// </summary>
public sealed record MediaReleaseCreatedEvent(
    MediaReleaseId MediaReleaseId,
    UserId UserId,
    string Title,
    string Artist,
    MediaFormat Format,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public MediaReleaseCreatedEvent(MediaReleaseId mediaReleaseId, UserId userId, string title, string artist, MediaFormat format)
        : this(mediaReleaseId, userId, title, artist, format, DateTime.UtcNow)
    {
    }
}
