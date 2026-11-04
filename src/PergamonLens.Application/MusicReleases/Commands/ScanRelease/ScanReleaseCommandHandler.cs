using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Commands.ScanRelease;

/// <summary>
/// Manejador del caso de uso de ingesta y escaneo visual mediante IA multimodal con enriquecimiento automático
/// polimórfico (Música vía Discogs, Libros vía OpenLibrary y Videojuegos).
/// </summary>
public sealed class ScanReleaseCommandHandler : IRequestHandler<ScanReleaseCommand, MusicReleaseDto>
{
    private readonly IVisionService _visionService;
    private readonly IMusicReleaseRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDiscogsService _discogsService;
    private readonly IBookCatalogService? _bookCatalogService;
    private readonly ILogger<ScanReleaseCommandHandler> _logger;

    public ScanReleaseCommandHandler(
        IVisionService visionService,
        IMusicReleaseRepository repository,
        ICurrentUserService currentUserService,
        IDiscogsService discogsService,
        ILogger<ScanReleaseCommandHandler>? logger = null,
        IBookCatalogService? bookCatalogService = null)
    {
        _visionService = visionService;
        _repository = repository;
        _currentUserService = currentUserService;
        _discogsService = discogsService;
        _logger = logger ?? NullLogger<ScanReleaseCommandHandler>.Instance;
        _bookCatalogService = bookCatalogService;
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

        var visionFormat = MediaFormatExtensions.ParseFromText(extraction.DetectedFormat);

        bool isBook = MediaFormatExtensions.IsIsbn(barcode?.Value ?? extraction.DetectedBarcode)
            || extraction.Category == PhysicalMediaCategory.Book
            || visionFormat?.GetCategory() == MediaCategory.Books
            || (extraction.Category == PhysicalMediaCategory.Unknown && request.Format.GetCategory() == MediaCategory.Books);

        bool isVideoGame = !isBook && (
            extraction.Category == PhysicalMediaCategory.VideoGame
            || visionFormat?.GetCategory() == MediaCategory.VideoGames);

        if (isBook)
        {
            return await HandleBookScanAsync(
                userId,
                request.Format,
                extraction,
                barcode,
                catalogueNumber,
                visionFormat,
                cancellationToken);
        }

        if (isVideoGame)
        {
            return await HandleVideoGameScanAsync(
                userId,
                request.Format,
                extraction,
                barcode,
                catalogueNumber,
                visionFormat,
                cancellationToken);
        }

        string title = !string.IsNullOrWhiteSpace(extraction.DetectedTitle)
            ? extraction.DetectedTitle.Trim()
            : "Lanzamiento pendiente de identificación";

        string artist = !string.IsNullOrWhiteSpace(extraction.DetectedArtist)
            ? extraction.DetectedArtist.Trim()
            : "Artista desconocido";

        DiscogsReleaseDetails? discogsDetails = null;
        string? discogsFormatCandidate = null;

        // 1. Si se detectó código de barras pero no título/artista (típico de Fase 1 local), consultamos Discogs por código de barras
        if (barcode is not null && string.IsNullOrWhiteSpace(extraction.DetectedTitle))
        {
            try
            {
                _logger.LogInformation("Consultando Discogs automáticamente para el código de barras {Barcode}...", barcode.Value);
                var matches = await _discogsService.SearchByBarcodeAsync(barcode, cancellationToken);
                var firstMatch = matches.FirstOrDefault();
                if (firstMatch is not null)
                {
                    _logger.LogInformation("Coincidencia en Discogs encontrada por código de barras: '{Title}' de '{Artist}' (ID: {ReleaseId})",
                        firstMatch.Title, firstMatch.Artist, firstMatch.ReleaseId);

                    discogsDetails = await _discogsService.GetReleaseDetailsAsync(firstMatch.ReleaseId, cancellationToken);
                    title = discogsDetails?.Title ?? firstMatch.Title;
                    artist = discogsDetails?.Artist ?? firstMatch.Artist;
                    discogsFormatCandidate = discogsDetails?.Format ?? firstMatch.Format;
                }
                else
                {
                    _logger.LogInformation("No se encontraron coincidencias en Discogs para el código de barras {Barcode}", barcode.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error no fatal al consultar Discogs para el código de barras {Barcode}", barcode.Value);
            }
        }

        // 2. Si no se obtuvo de código de barras pero la IA visual reconoció Artista y Álbum (Fase 2 de portada),
        // consultamos Discogs automáticamente para obtener carátula oficial, tracklist y metadatos canónicos
        if (discogsDetails is null &&
            !string.IsNullOrWhiteSpace(extraction.DetectedArtist) &&
            !string.IsNullOrWhiteSpace(extraction.DetectedTitle))
        {
            try
            {
                _logger.LogInformation("Consultando Discogs automáticamente para portada identificada: '{Artist}' - '{Title}'...",
                    extraction.DetectedArtist, extraction.DetectedTitle);

                var matches = await _discogsService.SearchByQueryAsync(
                    extraction.DetectedArtist,
                    extraction.DetectedTitle,
                    cancellationToken);

                var firstMatch = matches.FirstOrDefault();
                if (firstMatch is not null)
                {
                    _logger.LogInformation("Coincidencia en Discogs encontrada para portada: '{Title}' de '{Artist}' (ID: {ReleaseId})",
                        firstMatch.Title, firstMatch.Artist, firstMatch.ReleaseId);

                    discogsDetails = await _discogsService.GetReleaseDetailsAsync(firstMatch.ReleaseId, cancellationToken);
                    if (discogsDetails is not null)
                    {
                        title = discogsDetails.Title;
                        artist = discogsDetails.Artist;
                        discogsFormatCandidate = discogsDetails.Format ?? firstMatch.Format;
                    }
                }
                else
                {
                    _logger.LogInformation("No se encontraron coincidencias en Discogs para '{Artist}' - '{Title}'",
                        extraction.DetectedArtist, extraction.DetectedTitle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error no fatal al consultar Discogs para '{Artist}' - '{Title}'",
                    extraction.DetectedArtist, extraction.DetectedTitle);
            }
        }

        // Deducción inteligente del soporte físico (Discogs > IA Vision > Formato solicitado por defecto)
        string? formatCandidate = discogsFormatCandidate ?? extraction.DetectedFormat;
        var resolvedFormat = MediaFormatExtensions.ParseFromText(formatCandidate) ?? request.Format;

        var release = MusicRelease.Create(
            userId,
            title,
            artist,
            resolvedFormat,
            barcode,
            catalogueNumber,
            label: extraction.DetectedLabel ?? discogsDetails?.Label,
            notes: $"Análisis visual IA (confianza: {extraction.Confidence:P0}).");

        if (discogsDetails is not null)
        {
            ReleaseYear? year = null;
            if (discogsDetails.Year.HasValue && ReleaseYear.TryCreate(discogsDetails.Year.Value, out var parsedYear, out _))
            {
                year = parsedYear;
            }

            var tracks = discogsDetails.Tracks?
                .Select(t => Track.Create(t.Position, t.Title, t.Duration))
                .ToList();

            release.UpdateIdentifiedDetails(
                title,
                artist,
                barcode,
                catalogueNumber,
                releaseYear: year,
                label: discogsDetails.Label ?? extraction.DetectedLabel);

            release.EnrichFromDiscogs(
                discogsDetails.ReleaseId,
                discogsDetails.Label,
                year,
                discogsDetails.Country,
                discogsDetails.CoverImageUrl,
                tracks);
        }
        else if (!string.IsNullOrWhiteSpace(extraction.DetectedTitle) && !string.IsNullOrWhiteSpace(extraction.DetectedArtist))
        {
            ReleaseYear? yearFromVision = null;
            if (extraction.DetectedReleaseYear.HasValue &&
                ReleaseYear.TryCreate(extraction.DetectedReleaseYear.Value, out var parsedYear, out _))
            {
                yearFromVision = parsedYear;
            }

            release.UpdateIdentifiedDetails(
                extraction.DetectedTitle,
                extraction.DetectedArtist,
                barcode,
                catalogueNumber,
                releaseYear: yearFromVision,
                label: extraction.DetectedLabel);
        }
        else
        {
            // Sin datos reconocidos (ni por Discogs ni título/artista por IA):
            // Marcamos como Failed y NO persistimos en PostgreSQL para evitar registros huérfanos.
            release.MarkAsFailed("La IA visual no detectó metadatos legibles en la imagen.");
        }

        if (release.Status != ProcessingStatus.Failed)
        {
            await _repository.AddAsync(release, cancellationToken);
        }

        return release.ToDto();
    }

    private async Task<MusicReleaseDto> HandleBookScanAsync(
        UserId userId,
        MediaFormat requestedFormat,
        VisionExtractionResult extraction,
        Barcode? barcode,
        CatalogueNumber? catalogueNumber,
        MediaFormat? visionFormat,
        CancellationToken cancellationToken)
    {
        BookCatalogSearchResult? bookMatch = null;
        string? isbnCandidate = barcode?.Value ?? extraction.DetectedBarcode ?? catalogueNumber?.Value;

        if (_bookCatalogService is not null)
        {
            if (!string.IsNullOrWhiteSpace(isbnCandidate))
            {
                try
                {
                    _logger.LogInformation("Consultando catálogo OpenLibrary por ISBN {Isbn}...", isbnCandidate);
                    bookMatch = await _bookCatalogService.LookupByIsbnAsync(isbnCandidate, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error no fatal al consultar OpenLibrary por ISBN {Isbn}", isbnCandidate);
                }
            }

            if (bookMatch is null && !string.IsNullOrWhiteSpace(extraction.DetectedTitle))
            {
                try
                {
                    _logger.LogInformation("Consultando catálogo OpenLibrary por Autor/Título: '{Author}' - '{Title}'...",
                        extraction.DetectedArtist, extraction.DetectedTitle);
                    var results = await _bookCatalogService.SearchByQueryAsync(
                        extraction.DetectedArtist ?? string.Empty,
                        extraction.DetectedTitle,
                        cancellationToken);
                    bookMatch = results.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error no fatal al consultar OpenLibrary para '{Title}'", extraction.DetectedTitle);
                }
            }
        }

        string title = bookMatch?.Title
            ?? (!string.IsNullOrWhiteSpace(extraction.DetectedTitle) ? extraction.DetectedTitle.Trim() : "Libro pendiente de identificación");

        string author = bookMatch?.Author
            ?? (!string.IsNullOrWhiteSpace(extraction.DetectedArtist) ? extraction.DetectedArtist.Trim() : "Autor desconocido");

        var bookFormatFromCatalog = MediaFormatExtensions.ParseFromText(bookMatch?.Format);
        var resolvedFormat = bookFormatFromCatalog?.GetCategory() == MediaCategory.Books
            ? bookFormatFromCatalog.Value
            : visionFormat?.GetCategory() == MediaCategory.Books
                ? visionFormat.Value
                : requestedFormat.GetCategory() == MediaCategory.Books
                    ? requestedFormat
                    : MediaFormat.Paperback;

        if (catalogueNumber is null && !string.IsNullOrWhiteSpace(bookMatch?.Isbn) &&
            CatalogueNumber.TryCreate(bookMatch.Isbn, out var parsedIsbn, out _))
        {
            catalogueNumber = parsedIsbn;
        }

        var bookRelease = MusicRelease.CreateBook(
            userId,
            title,
            author,
            resolvedFormat,
            isbn13: barcode,
            isbn10: catalogueNumber,
            publisher: bookMatch?.Publisher ?? extraction.DetectedLabel,
            pagesCount: bookMatch?.PagesCount,
            coverImageUrl: bookMatch?.CoverImageUrl,
            notes: $"Análisis visual IA de libro (confianza: {extraction.Confidence:P0}).",
            externalCatalogId: bookMatch?.EditionKey);

        if (bookMatch is not null)
        {
            ReleaseYear? pubYear = null;
            if (bookMatch.PublishYear.HasValue && ReleaseYear.TryCreate(bookMatch.PublishYear.Value, out var parsedYear, out _))
            {
                pubYear = parsedYear;
            }

            bookRelease.EnrichFromBookCatalog(
                bookMatch.EditionKey,
                bookMatch.Title,
                bookMatch.Author,
                bookMatch.Publisher ?? extraction.DetectedLabel,
                pubYear,
                bookMatch.PagesCount,
                bookMatch.CoverImageUrl,
                catalogueNumber);
        }
        else if (!string.IsNullOrWhiteSpace(extraction.DetectedTitle))
        {
            ReleaseYear? yearFromVision = null;
            if (extraction.DetectedReleaseYear.HasValue &&
                ReleaseYear.TryCreate(extraction.DetectedReleaseYear.Value, out var parsedYear, out _))
            {
                yearFromVision = parsedYear;
            }

            bookRelease.UpdateIdentifiedDetails(
                title,
                author,
                barcode,
                catalogueNumber,
                releaseYear: yearFromVision,
                label: extraction.DetectedLabel);
        }
        else
        {
            bookRelease.MarkAsFailed("No se pudo identificar el libro ni por ISBN ni por portada.");
        }

        if (bookRelease.Status != ProcessingStatus.Failed)
        {
            await _repository.AddAsync(bookRelease, cancellationToken);
        }

        return bookRelease.ToDto();
    }

    private async Task<MusicReleaseDto> HandleVideoGameScanAsync(
        UserId userId,
        MediaFormat requestedFormat,
        VisionExtractionResult extraction,
        Barcode? barcode,
        CatalogueNumber? catalogueNumber,
        MediaFormat? visionFormat,
        CancellationToken cancellationToken)
    {
        string title = !string.IsNullOrWhiteSpace(extraction.DetectedTitle)
            ? extraction.DetectedTitle.Trim()
            : "Videojuego pendiente de identificación";

        string studio = !string.IsNullOrWhiteSpace(extraction.DetectedArtist)
            ? extraction.DetectedArtist.Trim()
            : (!string.IsNullOrWhiteSpace(extraction.DetectedLabel) ? extraction.DetectedLabel.Trim() : "Estudio desconocido");

        var resolvedFormat = visionFormat?.GetCategory() == MediaCategory.VideoGames
            ? visionFormat.Value
            : requestedFormat.GetCategory() == MediaCategory.VideoGames
                ? requestedFormat
                : MediaFormat.OpticalDiscGame;

        var gameRelease = MusicRelease.CreateVideoGame(
            userId,
            title,
            studio,
            resolvedFormat,
            platform: extraction.DetectedPlatform,
            barcode: barcode,
            productCode: catalogueNumber,
            publisher: extraction.DetectedLabel,
            notes: $"Análisis visual IA de videojuego (confianza: {extraction.Confidence:P0}).");

        if (!string.IsNullOrWhiteSpace(extraction.DetectedTitle))
        {
            ReleaseYear? yearFromVision = null;
            if (extraction.DetectedReleaseYear.HasValue &&
                ReleaseYear.TryCreate(extraction.DetectedReleaseYear.Value, out var parsedYear, out _))
            {
                yearFromVision = parsedYear;
            }

            gameRelease.UpdateIdentifiedDetails(
                title,
                studio,
                barcode,
                catalogueNumber,
                releaseYear: yearFromVision,
                label: extraction.DetectedLabel,
                platform: extraction.DetectedPlatform);
        }
        else
        {
            gameRelease.MarkAsFailed("La IA visual no detectó metadatos legibles en la carátula del videojuego.");
        }

        if (gameRelease.Status != ProcessingStatus.Failed)
        {
            await _repository.AddAsync(gameRelease, cancellationToken);
        }

        return gameRelease.ToDto();
    }
}
