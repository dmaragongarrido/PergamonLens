namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO de lectura completo para un lanzamiento musical físico en la colección.
/// </summary>
public sealed record MusicReleaseDto(
    Guid Id,
    string UserId,
    string Format,
    string Title,
    string Artist,
    string? Label,
    int? ReleaseYear,
    string? Barcode,
    string? CatalogueNumber,
    string? Country,
    string? CoverImageUrl,
    int DiscsCount,
    string? VinylSize,
    string? VinylSpeed,
    string? MediaCondition,
    string? SleeveCondition,
    string Status,
    long? DiscogsReleaseId,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<TrackDto> Tracks);
