using MediatR;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MediaReleases.Commands.EnrichReleaseFromGameCatalog;

/// <summary>
/// Manejador del comando de enriquecimiento de videojuegos con datos del catálogo oficial.
/// </summary>
public sealed class EnrichReleaseFromGameCatalogCommandHandler : IRequestHandler<EnrichReleaseFromGameCatalogCommand, MediaReleaseDto>
{
    private readonly IMediaReleaseRepository _repository;

    public EnrichReleaseFromGameCatalogCommandHandler(IMediaReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MediaReleaseDto> Handle(EnrichReleaseFromGameCatalogCommand request, CancellationToken cancellationToken)
    {
        var id = new MediaReleaseId(request.ReleaseId);
        var release = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaRelease), request.ReleaseId);

        ReleaseYear? releaseYear = null;
        if (request.ReleaseYear.HasValue && ReleaseYear.TryCreate(request.ReleaseYear.Value, out var parsedYear, out _))
        {
            releaseYear = parsedYear;
        }

        release.EnrichFromGameCatalog(
            request.GameKey,
            request.Title,
            request.Developer,
            request.Publisher,
            releaseYear,
            request.Platform,
            request.CoverImageUrl,
            request.Genre,
            request.Overview);

        await _repository.UpdateAsync(release, cancellationToken);

        return release.ToDto();
    }
}

