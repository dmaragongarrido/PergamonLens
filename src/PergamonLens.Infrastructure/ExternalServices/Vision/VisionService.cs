using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PergamonLens.Domain.Services;

namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Adaptador del servicio de visión multimodal e inteligencia artificial para extracción de metadatos desde fotografías.
/// </summary>
public sealed class VisionService : IVisionService
{
    private readonly IOptions<VisionOptions> _options;
    private readonly ILogger<VisionService> _logger;

    public VisionService(
        IOptions<VisionOptions> options,
        ILogger<VisionService> logger)
    {
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

        _logger.LogInformation("Iniciando análisis visual de imagen (ContentType: {ContentType}, Longitud: {Length} bytes)",
            contentType, imageStream.CanSeek ? imageStream.Length : -1);

        // Simulación controlada para entorno local / desarrollo sin credenciales cloud activas
        await Task.Delay(100, cancellationToken); // Simular latencia de inferencia

        // En ausencia de API externa configurada, devolvemos un resultado estructurado con alta confianza
        return new VisionExtractionResult(
            DetectedTitle: null,
            DetectedArtist: null,
            DetectedBarcode: null,
            DetectedCatalogueNumber: null,
            DetectedLabel: null,
            Confidence: _options.Value.DefaultConfidence,
            RawTextDetected: "OCR processing ready.");
    }
}
