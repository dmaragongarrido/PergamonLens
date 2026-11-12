using MediatR;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MediaReleases.Commands.EnrichReleaseFromBookCatalog;

/// <summary>
/// Manejador del comando de enriquecimiento bibliográfico con datos del catálogo oficial.
/// </summary>
public sealed class EnrichReleaseFromBookCatalogCommandHandler : IRequestHandler<EnrichReleaseFromBookCatalogCommand, MediaReleaseDto>
{
    private readonly IMediaReleaseRepository _repository;

    public EnrichReleaseFromBookCatalogCommandHandler(IMediaReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MediaReleaseDto> Handle(EnrichReleaseFromBookCatalogCommand request, CancellationToken cancellationToken)
    {
        var id = new MediaReleaseId(request.ReleaseId);
        var release = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaRelease), request.ReleaseId);

        ReleaseYear? publishYear = null;
        if (request.PublishYear.HasValue && ReleaseYear.TryCreate(request.PublishYear.Value, out var parsedYear, out _))
        {
            publishYear = parsedYear;
        }

        CatalogueNumber? isbn = null;
        if (!string.IsNullOrWhiteSpace(request.Isbn) && CatalogueNumber.TryCreate(request.Isbn, out var parsedIsbn, out _))
        {
            isbn = parsedIsbn;
        }

        release.EnrichFromBookCatalog(
            request.EditionKey,
            request.Title,
            request.Author,
            request.Publisher,
            publishYear,
            request.PagesCount,
            request.CoverImageUrl,
            isbn,
            request.Genre,
            request.Overview);

        await _repository.UpdateAsync(release, cancellationToken);

        return release.ToDto();
    }
}
