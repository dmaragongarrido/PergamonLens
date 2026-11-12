using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Services;

namespace PergamonLens.Application.MediaReleases.Queries.SearchGameCatalog;

/// <summary>
/// Manejador de búsqueda en el catálogo oficial de videojuegos.
/// </summary>
public sealed class SearchGameCatalogQueryHandler : IRequestHandler<SearchGameCatalogQuery, IReadOnlyList<GameCatalogSearchResultDto>>
{
    private readonly IGameCatalogService _gameCatalogService;

    public SearchGameCatalogQueryHandler(IGameCatalogService gameCatalogService)
    {
        _gameCatalogService = gameCatalogService;
    }

    public async Task<IReadOnlyList<GameCatalogSearchResultDto>> Handle(
        SearchGameCatalogQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.FreeText))
        {
            var freeResults = await _gameCatalogService.SearchByFreeTextAsync(request.FreeText.Trim(), cancellationToken);
            return freeResults.Select(r => r.ToDto()).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var results = await _gameCatalogService.SearchByQueryAsync(
                request.Title.Trim(),
                request.PlatformOrStudio?.Trim(),
                cancellationToken);
            return results.Select(r => r.ToDto()).ToList();
        }

        return [];
    }
}

