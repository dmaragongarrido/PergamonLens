using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Queries.SearchGameCatalog;

/// <summary>
/// Consulta para buscar videojuegos físicos en el catálogo oficial por título, estudio/plataforma o texto libre.
/// </summary>
public sealed record SearchGameCatalogQuery(
    string? Title = null,
    string? PlatformOrStudio = null,
    string? FreeText = null) : IRequest<IReadOnlyList<GameCatalogSearchResultDto>>;

