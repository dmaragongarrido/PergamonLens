namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO con los datos de una edición bibliográfica hallada en el catálogo oficial (OpenLibrary).
/// </summary>
public sealed record BookCatalogSearchResultDto(
    string EditionKey,
    string Title,
    string Author,
    int? PublishYear,
    string? Publisher,
    string? Isbn,
    int? PagesCount,
    string? CoverImageUrl,
    string? Format = null,
    string? Genre = null,
    string? Overview = null);

