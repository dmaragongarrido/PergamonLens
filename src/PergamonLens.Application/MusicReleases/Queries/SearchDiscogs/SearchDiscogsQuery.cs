using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Queries.SearchDiscogs;

/// <summary>
/// Consulta para buscar referencias y ediciones en el catálogo canónico de Discogs.
/// </summary>
public sealed record SearchDiscogsQuery(
    string? Barcode = null,
    string? CatalogueNumber = null,
    string? Artist = null,
    string? Title = null) : IRequest<IReadOnlyList<DiscogsSearchResultDto>>;

