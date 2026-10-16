using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando el procesamiento de reconocimiento o enriquecimiento falla.
/// </summary>
public sealed record VinylRecordFailedEvent(
    VinylRecordId VinylRecordId,
    string Reason,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public VinylRecordFailedEvent(VinylRecordId vinylRecordId, string reason)
        : this(vinylRecordId, reason, DateTime.UtcNow)
    {
    }
}
