namespace PergamonLens.Application.Common.Interfaces;

/// <summary>
/// Abstracción para obtener la identidad del usuario autenticado en la sesión o petición actual.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}
