using Microsoft.Extensions.Logging;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Implementación de escaneo determinista local de códigos de barras mediante ZXing.Net y SkiaSharp.
/// Se ejecuta en memoria en ~2 ms con coste cero de tokens.
/// </summary>
public sealed class LocalBarcodeScanner : IBarcodeScanner
{
    private readonly ILogger<LocalBarcodeScanner> _logger;

    private static readonly DecodingOptions DefaultOptions = new()
    {
        TryHarder = true,
        PossibleFormats = new List<BarcodeFormat>
        {
            BarcodeFormat.EAN_13,
            BarcodeFormat.EAN_8,
            BarcodeFormat.UPC_A,
            BarcodeFormat.UPC_E,
            BarcodeFormat.CODE_128,
            BarcodeFormat.CODE_39,
            BarcodeFormat.QR_CODE
        }
    };

    public LocalBarcodeScanner(ILogger<LocalBarcodeScanner> logger)
    {
        _logger = logger;
    }

    public BarcodeScanResult? Scan(Stream imageStream)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        if (!imageStream.CanRead)
        {
            return null;
        }

        try
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            // Usar disposeManagedStream: false explícitamente para evitar que SkiaSharp
            // cierre o libere el Stream subyacente que luego necesita la Fase 2 (Gemini).
            using var managedStream = new SKManagedStream(imageStream, disposeManagedStream: false);
            using var skBitmap = SKBitmap.Decode(managedStream);
            if (skBitmap == null)
            {
                _logger.LogDebug("No se pudo decodificar el stream como bitmap SkiaSharp válido.");
                return null;
            }

            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = DefaultOptions
            };

            var result = reader.Decode(skBitmap);
            if (result != null && !string.IsNullOrWhiteSpace(result.Text))
            {
                _logger.LogInformation("Código de barras detectado en memoria con éxito: {Barcode} (Formato: {Format})",
                    result.Text, result.BarcodeFormat);

                return new BarcodeScanResult(result.Text.Trim(), result.BarcodeFormat.ToString());
            }

            _logger.LogDebug("No se detectó ningún código de barras en la imagen proporcionada.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error no fatal al intentar decodificar código de barras local con ZXing.Net.");
            return null;
        }
        finally
        {
            try
            {
                if (imageStream.CanRead && imageStream.CanSeek)
                {
                    imageStream.Position = 0;
                }
            }
            catch (ObjectDisposedException)
            {
                // Protección defensiva en caso de streams efímeros de ASP.NET Core
            }
        }
    }
}
