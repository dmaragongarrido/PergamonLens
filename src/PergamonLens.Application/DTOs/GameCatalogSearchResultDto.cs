namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO con los datos de una edición de videojuego encontrada en el catálogo oficial.
/// </summary>
public sealed record GameCatalogSearchResultDto(
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

