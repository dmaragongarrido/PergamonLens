namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Opciones de configuración para el servicio de Visión e Inteligencia Artificial Multimodal (Gemini 2.0 Flash / Local).
/// </summary>
public sealed class VisionOptions
{
    public const string SectionName = "Vision";

    public string Provider { get; set; } = "Gemini";
    public string? ApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.0-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";
    public double DefaultConfidence { get; set; } = 0.90;

    /// <summary>
    /// Clave de API activa (admite GeminiApiKey o ApiKey desde appsettings o .NET User Secrets).
    /// </summary>
    public string? EffectiveApiKey => !string.IsNullOrWhiteSpace(GeminiApiKey) ? GeminiApiKey : ApiKey;
}
