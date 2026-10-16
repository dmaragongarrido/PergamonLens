using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido al registrar o escanear un nuevo disco de vinilo en el sistema.
/// </summary>
public sealed record VinylRecordCreatedEvent(
    VinylRecordId VinylRecordId,
    string Title,
    string Artist,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public VinylRecordCreatedEvent(VinylRecordId vinylRecordId, string title, string artist)
        : this(vinylRecordId, title, artist, DateTime.UtcNow)
    {
    }
}
