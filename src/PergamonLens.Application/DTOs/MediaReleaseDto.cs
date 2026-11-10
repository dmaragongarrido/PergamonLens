namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO de lectura completo para un ejemplar físico en la bóveda (Música, Libro o Videojuego).
/// </summary>
public sealed record MediaReleaseDto(
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
    IReadOnlyList<TrackDto> Tracks,
    string Category = "Music",
    int? PagesCount = null,
    string? Platform = null,
    string? ExternalCatalogId = null,
    string? Genre = null,
    string? Overview = null);
