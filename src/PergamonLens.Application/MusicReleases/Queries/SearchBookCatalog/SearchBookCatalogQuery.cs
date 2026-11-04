using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Queries.SearchBookCatalog;

/// <summary>
/// Consulta para buscar ediciones bibliográficas en el catálogo oficial (OpenLibrary) por ISBN, autor+título o texto libre.
/// </summary>
public sealed record SearchBookCatalogQuery(
    string? Isbn = null,
    string? Author = null,
    string? Title = null,
    string? FreeText = null) : IRequest<IReadOnlyList<BookCatalogSearchResultDto>>;

