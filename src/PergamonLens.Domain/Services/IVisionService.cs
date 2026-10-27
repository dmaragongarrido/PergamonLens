namespace PergamonLens.Domain.Services;

/// <summary>
/// Categoría del medio físico coleccionable detectado en el análisis visual.
/// </summary>
public enum PhysicalMediaCategory
{
    Music,
    Book,
    VideoGame,
    Unknown
}

/// <summary>
/// Resultado del análisis visual de una imagen de portada, lomo o código de barras.
/// </summary>
public sealed record VisionExtractionResult(
    string? DetectedTitle,
    string? DetectedArtist,
    string? DetectedBarcode,
    string? DetectedCatalogueNumber,
    string? DetectedLabel,
    double Confidence,
    string? RawTextDetected = null,
    PhysicalMediaCategory Category = PhysicalMediaCategory.Music,
    string? DetectedFormat = null,
    string? DetectedPlatform = null,
    int? DetectedReleaseYear = null,
    string? SuggestedSearchQuery = null)
{
    /// <summary>
    /// Alias semántico para creadores en medios distintos a música (Autor en libros, Desarrollador en videojuegos).
    /// </summary>
    public string? DetectedCreator => DetectedArtist;
}

/// <summary>
/// Contrato para el servicio de inteligencia artificial visual multimodal (OCR y detección de entidades en portadas/lomos).
/// </summary>
public interface IVisionService
{
    Task<VisionExtractionResult> AnalyzeImageAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default);
}
