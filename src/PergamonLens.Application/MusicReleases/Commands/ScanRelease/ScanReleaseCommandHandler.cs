using MediatR;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Commands.ScanRelease;

/// <summary>
/// Manejador del caso de uso de ingesta y escaneo visual mediante IA multimodal.
/// </summary>
public sealed class ScanReleaseCommandHandler : IRequestHandler<ScanReleaseCommand, MusicReleaseDto>
{
    private readonly IVisionService _visionService;
    private readonly IMusicReleaseRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public ScanReleaseCommandHandler(
        IVisionService visionService,
        IMusicReleaseRepository repository,
        ICurrentUserService currentUserService)
    {
        _visionService = visionService;
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<MusicReleaseDto> Handle(ScanReleaseCommand request, CancellationToken cancellationToken)
    {
        var rawUserId = !string.IsNullOrWhiteSpace(request.UserId)
            ? request.UserId
            : _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(rawUserId))
        {
            throw new UnauthorizedAccessException("Se requiere un usuario autenticado para registrar un lanzamiento musical.");
        }

        var userId = UserId.From(rawUserId);

        var extraction = await _visionService.AnalyzeImageAsync(
            request.ImageStream,
            request.ContentType,
            cancellationToken);

        Barcode? barcode = null;
        if (!string.IsNullOrWhiteSpace(extraction.DetectedBarcode) &&
            Barcode.TryCreate(extraction.DetectedBarcode, out var parsedBarcode, out _))
        {
            barcode = parsedBarcode;
        }

        CatalogueNumber? catalogueNumber = null;
        if (!string.IsNullOrWhiteSpace(extraction.DetectedCatalogueNumber) &&
            CatalogueNumber.TryCreate(extraction.DetectedCatalogueNumber, out var parsedCatNumber, out _))
        {
            catalogueNumber = parsedCatNumber;
        }

        string title = !string.IsNullOrWhiteSpace(extraction.DetectedTitle)
            ? extraction.DetectedTitle.Trim()
            : "Lanzamiento pendiente de identificación";

        string artist = !string.IsNullOrWhiteSpace(extraction.DetectedArtist)
            ? extraction.DetectedArtist.Trim()
            : "Artista desconocido";

        var release = MusicRelease.Create(
            userId,
            title,
            artist,
            request.Format,
            barcode,
            catalogueNumber,
            label: extraction.DetectedLabel,
            notes: $"Análisis visual IA (confianza: {extraction.Confidence:P0}).");

        bool hasRecognizedMetadata = !string.IsNullOrWhiteSpace(extraction.DetectedTitle) ||
                                     !string.IsNullOrWhiteSpace(extraction.DetectedArtist) ||
                                     barcode is not null ||
                                     catalogueNumber is not null;

        if (extraction.Confidence < 0.2 && !hasRecognizedMetadata)
        {
            release.MarkAsFailed("La IA visual no detectó metadatos legibles en la imagen.");
        }
        else if (!string.IsNullOrWhiteSpace(extraction.DetectedTitle) && !string.IsNullOrWhiteSpace(extraction.DetectedArtist))
        {
            release.UpdateIdentifiedDetails(
                extraction.DetectedTitle,
                extraction.DetectedArtist,
                barcode,
                catalogueNumber,
                label: extraction.DetectedLabel);
        }

        await _repository.AddAsync(release, cancellationToken);

        return release.ToDto();
    }
}
