using PergamonLens.Domain.Common;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Events;

/// <summary>
/// Evento de dominio emitido cuando el coleccionista valida y custodia un vinilo en su colección personal.
/// </summary>
public sealed record VinylRecordConfirmedEvent(
    VinylRecordId VinylRecordId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public VinylRecordConfirmedEvent(VinylRecordId vinylRecordId)
        : this(vinylRecordId, DateTime.UtcNow)
    {
    }
}
