namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// DTO que mapea el resultado estructurado de la inferencia multimodal de Gemini 2.0 Flash (Structured Outputs).
/// </summary>
public sealed record GeminiVisionAnalysisDto(
    string? MediaCategory,
    string? Title,
    string? Creator,
    string? Format,
    string? Platform,
    int? ReleaseYear,
    string? PublisherOrLabel,
    string? CatalogueOrIdentifier,
    string? Barcode,
    double Confidence,
    string? SuggestedSearchQuery);

/// <summary>
/// Contrato del cliente HTTP para inferencia multimodal con Google Gemini.
/// </summary>
public interface IGeminiVisionClient
{
    Task<GeminiVisionAnalysisDto?> AnalyzeMediaCoverAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default);
}

