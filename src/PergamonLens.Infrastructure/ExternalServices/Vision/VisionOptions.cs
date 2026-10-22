namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Opciones de configuración para el servicio de Visión e Inteligencia Artificial Multimodal.
/// </summary>
public sealed class VisionOptions
{
    public const string SectionName = "Vision";

    public string Provider { get; set; } = "Local";
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public double DefaultConfidence { get; set; } = 0.90;
}
