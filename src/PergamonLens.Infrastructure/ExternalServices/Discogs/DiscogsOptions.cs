namespace PergamonLens.Infrastructure.ExternalServices.Discogs;

/// <summary>
/// Opciones de configuración para el cliente de la API de Discogs.
/// </summary>
public sealed class DiscogsOptions
{
    public const string SectionName = "Discogs";

    public string BaseUrl { get; set; } = "https://api.discogs.com/";
    public string UserAgent { get; set; } = "PergamonLens/1.0 (+https://github.com/dmaragongarrido/PergamonLens)";
    public string? Token { get; set; }
}
