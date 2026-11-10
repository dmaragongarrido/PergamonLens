using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Services;

namespace PergamonLens.Application.MediaReleases.Queries.SearchBookCatalog;

/// <summary>
/// Manejador de búsqueda en el catálogo bibliográfico oficial (OpenLibrary).
/// </summary>
public sealed class SearchBookCatalogQueryHandler : IRequestHandler<SearchBookCatalogQuery, IReadOnlyList<BookCatalogSearchResultDto>>
{
    private readonly IBookCatalogService _bookCatalogService;

    public SearchBookCatalogQueryHandler(IBookCatalogService bookCatalogService)
    {
        _bookCatalogService = bookCatalogService;
    }

    public async Task<IReadOnlyList<BookCatalogSearchResultDto>> Handle(
        SearchBookCatalogQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Isbn))
        {
            var single = await _bookCatalogService.LookupByIsbnAsync(request.Isbn.Trim(), cancellationToken);
            if (single is not null)
            {
                return [single.ToDto()];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.FreeText))
        {
            var freeResults = await _bookCatalogService.SearchByFreeTextAsync(request.FreeText.Trim(), cancellationToken);
            return freeResults.Select(r => r.ToDto()).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Author))
        {
            var queryResults = await _bookCatalogService.SearchByQueryAsync(
                request.Author?.Trim() ?? string.Empty,
                request.Title?.Trim() ?? string.Empty,
                cancellationToken);
            return queryResults.Select(r => r.ToDto()).ToList();
        }

        return [];
    }
}

