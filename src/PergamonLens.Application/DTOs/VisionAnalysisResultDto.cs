namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO con los datos detectados por el motor de visión e inteligencia artificial multimodal.
/// </summary>
public sealed record VisionAnalysisResultDto(
    string? DetectedTitle,
    string? DetectedArtist,
    string? DetectedBarcode,
    string? DetectedCatalogueNumber,
    string? DetectedLabel,
    double Confidence,
    string? RawTextDetected);

