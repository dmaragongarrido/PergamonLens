using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Commands.EnrichReleaseFromGameCatalog;

/// <summary>
/// Comando para enriquecer un videojuego físico de la bóveda con los datos de una edición seleccionada en el catálogo oficial.
/// </summary>
public sealed record EnrichReleaseFromGameCatalogCommand(
    Guid ReleaseId,
    string GameKey,
    string? Title = null,
    string? Developer = null,
    string? Publisher = null,
    int? ReleaseYear = null,
    string? Platform = null,
    string? CoverImageUrl = null) : IRequest<MusicReleaseDto>;

