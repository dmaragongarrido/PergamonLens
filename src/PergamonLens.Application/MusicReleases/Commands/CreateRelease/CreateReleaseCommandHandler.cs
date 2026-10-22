using MediatR;
using PergamonLens.Application.Common.Interfaces;
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
    private readonly ICurrentUserService _currentUserService;

    public CreateReleaseCommandHandler(
        IMusicReleaseRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<MusicReleaseDto> Handle(CreateReleaseCommand request, CancellationToken cancellationToken)
    {
        var rawUserId = !string.IsNullOrWhiteSpace(request.UserId)
            ? request.UserId
            : _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(rawUserId))
        {
            throw new UnauthorizedAccessException("Se requiere un usuario autenticado para registrar un lanzamiento musical en la colección.");
        }

        var userId = UserId.From(rawUserId);

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
            userId,
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
