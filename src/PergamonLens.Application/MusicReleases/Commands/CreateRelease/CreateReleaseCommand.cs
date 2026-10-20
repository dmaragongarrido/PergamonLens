using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Enums;

namespace PergamonLens.Application.MusicReleases.Commands.CreateRelease;

/// <summary>
/// Comando para crear manualmente un lanzamiento musical en el catálogo.
/// </summary>
public sealed record CreateReleaseCommand(
    string Title,
    string Artist,
    MediaFormat Format = MediaFormat.Vinyl,
    string? Barcode = null,
    string? CatalogueNumber = null,
    int? ReleaseYear = null,
    int DiscsCount = 1,
    VinylSize? VinylSize = null,
    VinylSpeed? VinylSpeed = null,
    string? Label = null,
    string? Country = null,
    string? CoverImageUrl = null,
    string? Notes = null) : IRequest<MusicReleaseDto>;

