using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Services;

/// <summary>
/// Resumen de una edición bibliográfica encontrada en el catálogo oficial (OpenLibrary).
/// </summary>
public sealed record BookCatalogSearchResult(
    string EditionKey,
    string Title,
    string Author,
    int? PublishYear,
    string? Publisher,
    string? Isbn,
    int? PagesCount,
    string? CoverImageUrl,
    string? Format = null);

/// <summary>
///Contrato para el servicio de catálogo bibliográfico oficial (OpenLibrary API).
/// </summary>
public interface IBookCatalogService
{
    Task<BookCatalogSearchResult?> LookupByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCatalogSearchResult>> SearchByQueryAsync(
        string author,
        string title,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCatalogSearchResult>> SearchByFreeTextAsync(
        string query,
        CancellationToken cancellationToken = default);
}

