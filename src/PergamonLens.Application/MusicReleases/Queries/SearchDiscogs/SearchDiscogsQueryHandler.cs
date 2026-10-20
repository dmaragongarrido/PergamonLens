using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Queries.SearchDiscogs;

/// <summary>
/// Manejador para consultar el catálogo de Discogs según el criterio más específico disponible.
/// </summary>
public sealed class SearchDiscogsQueryHandler : IRequestHandler<SearchDiscogsQuery, IReadOnlyList<DiscogsSearchResultDto>>
{
    private readonly IDiscogsService _discogsService;

    public SearchDiscogsQueryHandler(IDiscogsService discogsService)
    {
        _discogsService = discogsService;
    }

    public async Task<IReadOnlyList<DiscogsSearchResultDto>> Handle(SearchDiscogsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscogsSearchResult> results;

        if (!string.IsNullOrWhiteSpace(request.Barcode) &&
            Barcode.TryCreate(request.Barcode, out var barcode, out _))
        {
            results = await _discogsService.SearchByBarcodeAsync(barcode, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.CatalogueNumber) &&
                 CatalogueNumber.TryCreate(request.CatalogueNumber, out var catalogueNumber, out _))
        {
            results = await _discogsService.SearchByCatalogueNumberAsync(catalogueNumber, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Artist) && !string.IsNullOrWhiteSpace(request.Title))
        {
            results = await _discogsService.SearchByQueryAsync(request.Artist, request.Title, cancellationToken);
        }
        else
        {
            results = [];
        }

        return results
            .Select(r => r.ToDto())
            .ToList();
    }
}

