using MediatR;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Commands.EnrichReleaseFromDiscogs;

/// <summary>
/// Manejador para el enriquecimiento de metadatos desde Discogs.
/// </summary>
public sealed class EnrichReleaseFromDiscogsCommandHandler : IRequestHandler<EnrichReleaseFromDiscogsCommand, MusicReleaseDto>
{
    private readonly IMusicReleaseRepository _repository;
    private readonly IDiscogsService _discogsService;

    public EnrichReleaseFromDiscogsCommandHandler(
        IMusicReleaseRepository repository,
        IDiscogsService discogsService)
    {
        _repository = repository;
        _discogsService = discogsService;
    }

    public async Task<MusicReleaseDto> Handle(EnrichReleaseFromDiscogsCommand request, CancellationToken cancellationToken)
    {
        var releaseId = new MusicReleaseId(request.ReleaseId);
        var release = await _repository.GetByIdAsync(releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(MusicRelease), request.ReleaseId);
        }

        var details = await _discogsService.GetReleaseDetailsAsync(request.DiscogsReleaseId, cancellationToken);
        if (details is null)
        {
            throw new NotFoundException($"No se encontró en Discogs la edición especificada con ID {request.DiscogsReleaseId}.");
        }

        ReleaseYear? year = null;
        if (details.Year.HasValue && ReleaseYear.TryCreate(details.Year.Value, out var parsedYear, out _))
        {
            year = parsedYear;
        }

        var tracks = details.Tracks?
            .Select(t => Track.Create(t.Position, t.Title, t.Duration))
            .ToList();

        release.EnrichFromDiscogs(
            details.ReleaseId,
            details.Label,
            year,
            details.Country,
            details.CoverImageUrl,
            tracks);

        await _repository.UpdateAsync(release, cancellationToken);

        return release.ToDto();
    }
}

