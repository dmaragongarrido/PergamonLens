using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Services;

/// <summary>
/// Resumen de un resultado de búsqueda en el catálogo canónico de Discogs.
/// </summary>
public sealed record DiscogsSearchResult(
    long ReleaseId,
    string Title,
    string Artist,
    int? Year,
    string? Label,
    string? CatalogueNumber,
    string? Country,
    string? CoverThumbUrl,
    string? Format = null);

/// <summary>
/// Metadatos detallados de una edición concreta en Discogs (Vinilo, CD, Casete, etc.).
/// </summary>
public sealed record DiscogsReleaseDetails(
    long ReleaseId,
    string Title,
    string Artist,
    int? Year,
    string? Label,
    string? CatalogueNumber,
    string? Country,
    string? CoverImageUrl,
    IReadOnlyList<DiscogsTrackItem> Tracks,
    string? Format = null);

/// <summary>
/// Representa un corte musical extraído de Discogs.
/// </summary>
public sealed record DiscogsTrackItem(
    string Position,
    string Title,
    TimeSpan? Duration);

/// <summary>
/// Contrato para el cliente del catálogo musical canónico de Discogs.
/// </summary>
public interface IDiscogsService
{
    Task<IReadOnlyList<DiscogsSearchResult>> SearchByBarcodeAsync(
        Barcode barcode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscogsSearchResult>> SearchByCatalogueNumberAsync(
        CatalogueNumber catalogueNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscogsSearchResult>> SearchByQueryAsync(
        string artist,
        string title,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscogsSearchResult>> SearchByFreeTextAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<DiscogsReleaseDetails?> GetReleaseDetailsAsync(
        long releaseId,
        CancellationToken cancellationToken = default);
}
