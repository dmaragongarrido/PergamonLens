using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Commands.CreateRelease;

/// <summary>
/// Manejador del comando de creación de un lanzamiento musical.
/// </summary>
public sealed class CreateReleaseCommandHandler : IRequestHandler<CreateReleaseCommand, MusicReleaseDto>
{
    private readonly IMusicReleaseRepository _repository;

    public CreateReleaseCommandHandler(IMusicReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MusicReleaseDto> Handle(CreateReleaseCommand request, CancellationToken cancellationToken)
    {
        Barcode? barcode = !string.IsNullOrWhiteSpace(request.Barcode)
            ? Barcode.Create(request.Barcode)
            : null;

        CatalogueNumber? catalogueNumber = !string.IsNullOrWhiteSpace(request.CatalogueNumber)
            ? CatalogueNumber.Create(request.CatalogueNumber)
            : null;

        ReleaseYear? releaseYear = request.ReleaseYear.HasValue
            ? ReleaseYear.Create(request.ReleaseYear.Value)
            : null;

        var release = MusicRelease.Create(
            request.Title,
            request.Artist,
            request.Format,
            barcode,
            catalogueNumber,
            releaseYear,
            request.DiscsCount,
            request.VinylSize,
            request.VinylSpeed,
            request.Label,
            request.Country,
            request.CoverImageUrl,
            request.Notes);

        await _repository.AddAsync(release, cancellationToken);

        return release.ToDto();
    }
}

