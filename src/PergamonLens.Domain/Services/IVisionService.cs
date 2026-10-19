namespace PergamonLens.Domain.Services;

/// <summary>
/// Resultado del análisis visual de una imagen de portada o código de barras.
/// </summary>
public sealed record VisionExtractionResult(
    string? DetectedTitle,
    string? DetectedArtist,
    string? DetectedBarcode,
    string? DetectedCatalogueNumber,
    string? DetectedLabel,
    double Confidence,
    string? RawTextDetected = null);

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

