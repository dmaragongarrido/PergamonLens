namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO con el resumen de un resultado obtenido de la búsqueda en Discogs.
/// </summary>
public sealed record DiscogsSearchResultDto(
    long ReleaseId,
    string Title,
    string Artist,
    int? Year,
    string? Label,
    string? CatalogueNumber,
    string? Country,
    string? CoverThumbUrl);

