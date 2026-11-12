namespace PergamonLens.Domain.Services;

/// <summary>
/// Resumen de una edición de videojuego encontrada en el catálogo oficial.
/// </summary>
public sealed record GameCatalogSearchResult(
    string GameKey,
    string Title,
    string Developer,
    int? ReleaseYear,
    string? Publisher,
    string? Platform,
    string? CoverImageUrl,
    string? Format = null,
    string? Genre = null,
    string? Overview = null);

/// <summary>
/// Contrato para el servicio de catálogo de videojuegos (Consola, Retro y PC).
/// </summary>
public interface IGameCatalogService
{
    Task<IReadOnlyList<GameCatalogSearchResult>> SearchByQueryAsync(
        string title,
        string? platformOrStudio = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameCatalogSearchResult>> SearchByFreeTextAsync(
        string query,
        CancellationToken cancellationToken = default);
}

