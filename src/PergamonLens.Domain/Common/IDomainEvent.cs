namespace PergamonLens.Domain.Common;

/// <summary>
/// Representa un evento de dominio generado dentro del modelo de negocio.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}

