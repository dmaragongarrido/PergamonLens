namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO con los datos detectados por el motor de visión e inteligencia artificial multimodal.
/// Soporta metadatos polimórficos para música, libros y videojuegos.
/// </summary>
public sealed record VisionAnalysisResultDto(
    string? DetectedTitle,
    string? DetectedArtist,
    string? DetectedBarcode,
    string? DetectedCatalogueNumber,
    string? DetectedLabel,
    double Confidence,
    string? RawTextDetected,
    string Category = "Music",
    string? DetectedFormat = null,
    string? DetectedPlatform = null,
    int? DetectedReleaseYear = null,
    string? SuggestedSearchQuery = null);
