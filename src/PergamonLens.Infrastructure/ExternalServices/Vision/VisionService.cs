using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PergamonLens.Domain.Services;

namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Adaptador del servicio de visión multimodal e inteligencia artificial para extracción de metadatos desde fotografías.
/// Implementa un pipeline híbrido y polimórfico en dos fases:
///   - Fase 1: Escáner determinista local en memoria para códigos de barras (ZXing.Net / SkiaSharp, ~2 ms, 0 tokens),
///             con detección automática de prefijos Bookland (ISBN 978/979) para libros.
///   - Fase 2: Inferencia multimodal con Google Gemini 2.0 Flash (Structured Outputs) para reconocimiento de carátulas
///             de música (vinilos/CDs), libros y videojuegos.
/// </summary>
public sealed class VisionService : IVisionService
{
    private readonly IBarcodeScanner _barcodeScanner;
    private readonly IGeminiVisionClient _geminiVisionClient;
    private readonly IOptions<VisionOptions> _options;
    private readonly ILogger<VisionService> _logger;

    public VisionService(
        IBarcodeScanner barcodeScanner,
        IGeminiVisionClient geminiVisionClient,
        IOptions<VisionOptions> options,
        ILogger<VisionService> logger)
    {
        _barcodeScanner = barcodeScanner;
        _geminiVisionClient = geminiVisionClient;
        _options = options;
        _logger = logger;
    }

    public async Task<VisionExtractionResult> AnalyzeImageAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (!imageStream.CanRead)
        {
            throw new ArgumentException("El stream de la imagen no es legible.", nameof(imageStream));
        }

        _logger.LogInformation("Iniciando pipeline de análisis visual de imagen (ContentType: {ContentType})", contentType);

        // Copiar el stream entrante (ej. ReferenceReadStream de ASP.NET Core) a un MemoryStream en memoria
        // para aislarlo completamente y garantizar rebobinado seguro entre la Fase 1 y la Fase 2
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        // FASE 1: Detección determinista local de códigos de barras (0 tokens, ~2 ms)
        var barcodeResult = _barcodeScanner.Scan(memoryStream);
        if (barcodeResult != null)
        {
            var category = DetermineCategoryFromBarcode(barcodeResult.Barcode);

            _logger.LogInformation("Código de barras detectado en Fase 1 determinista: {Barcode} ({Format}, Categoría inferida: {Category}). Omitiendo IA cloud.",
                barcodeResult.Barcode, barcodeResult.BarcodeFormat, category);

            return new VisionExtractionResult(
                DetectedTitle: null,
                DetectedArtist: null,
                DetectedBarcode: barcodeResult.Barcode,
                DetectedCatalogueNumber: null,
                DetectedLabel: null,
                Confidence: 1.0,
                RawTextDetected: $"Barcode format: {barcodeResult.BarcodeFormat}",
                Category: category);
        }

        _logger.LogInformation("Fase 1 no detectó código de barras. Procediendo a Fase 2 (Inferencia multimodal con Gemini 2.0 Flash).");

        memoryStream.Position = 0;

        // FASE 2: Inferencia multimodal cloud con Gemini 2.0 Flash
        var geminiAnalysis = await _geminiVisionClient.AnalyzeMediaCoverAsync(memoryStream, contentType, cancellationToken);
        if (geminiAnalysis != null)
        {
            var category = ParsePhysicalMediaCategory(geminiAnalysis.MediaCategory);

            _logger.LogInformation("Inferencia multimodal completada con éxito. Título: '{Title}', Creador: '{Creator}', Categoría: {Category}, Confianza: {Confidence:P0}",
                geminiAnalysis.Title, geminiAnalysis.Creator, category, geminiAnalysis.Confidence);

            return new VisionExtractionResult(
                DetectedTitle: geminiAnalysis.Title,
                DetectedArtist: geminiAnalysis.Creator,
                DetectedBarcode: geminiAnalysis.Barcode,
                DetectedCatalogueNumber: geminiAnalysis.CatalogueOrIdentifier,
                DetectedLabel: geminiAnalysis.PublisherOrLabel,
                Confidence: geminiAnalysis.Confidence,
                RawTextDetected: $"Gemini Structured Output ({geminiAnalysis.MediaCategory})",
                Category: category,
                DetectedFormat: geminiAnalysis.Format,
                DetectedPlatform: geminiAnalysis.Platform,
                DetectedReleaseYear: geminiAnalysis.ReleaseYear,
                SuggestedSearchQuery: geminiAnalysis.SuggestedSearchQuery);
        }

        _logger.LogWarning("Fase 2 no obtuvo resultado estructurado de Gemini. Aplicando degradación elegante a fallback de desarrollo.");

        return new VisionExtractionResult(
            DetectedTitle: null,
            DetectedArtist: null,
            DetectedBarcode: null,
            DetectedCatalogueNumber: null,
            DetectedLabel: null,
            Confidence: _options.Value.DefaultConfidence,
            RawTextDetected: "No barcode detected. Multimodal inference fallback.",
            Category: PhysicalMediaCategory.Music);
    }

    private static PhysicalMediaCategory DetermineCategoryFromBarcode(string barcode)
    {
        // Regla determinista Bookland (ISBN-13): Prefijos internacionales 978 y 979
        if (barcode.Length == 13 && (barcode.StartsWith("978") || barcode.StartsWith("979")))
        {
            return PhysicalMediaCategory.Book;
        }

        // Por defecto en la versión actual (música y videojuegos comparten rangos UPC/EAN estándar)
        return PhysicalMediaCategory.Music;
    }

    private static PhysicalMediaCategory ParsePhysicalMediaCategory(string? mediaCategory)
    {
        return mediaCategory?.Trim().ToUpperInvariant() switch
        {
            "MUSIC" => PhysicalMediaCategory.Music,
            "BOOK" => PhysicalMediaCategory.Book,
            "VIDEOGAME" => PhysicalMediaCategory.VideoGame,
            _ => PhysicalMediaCategory.Unknown
        };
    }
}
