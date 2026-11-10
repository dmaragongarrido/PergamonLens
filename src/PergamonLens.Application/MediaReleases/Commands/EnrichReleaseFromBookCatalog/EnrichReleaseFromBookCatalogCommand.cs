using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MediaReleases.Commands.EnrichReleaseFromBookCatalog;

/// <summary>
/// Comando para enriquecer un libro de la bóveda con los datos de una edición seleccionada en OpenLibrary.
/// </summary>
public sealed record EnrichReleaseFromBookCatalogCommand(
    Guid ReleaseId,
    string EditionKey,
    string? Title = null,
    string? Author = null,
    string? Publisher = null,
    int? PublishYear = null,
    int? PagesCount = null,
    string? CoverImageUrl = null,
    string? Isbn = null) : IRequest<MediaReleaseDto>;

