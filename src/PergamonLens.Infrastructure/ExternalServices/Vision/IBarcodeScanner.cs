namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Resultado del escaneo determinista local de un código de barras.
/// </summary>
public sealed record BarcodeScanResult(
    string Barcode,
    string BarcodeFormat);

/// <summary>
/// Contrato para el escáner local de códigos de barras (ZXing.Net / SkiaSharp).
/// </summary>
public interface IBarcodeScanner
{
    BarcodeScanResult? Scan(Stream imageStream);
}

