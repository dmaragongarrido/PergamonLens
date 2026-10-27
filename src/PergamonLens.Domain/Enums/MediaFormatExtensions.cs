namespace PergamonLens.Domain.Enums;

/// <summary>
/// Extensiones y utilidades para la deducción e inferencia de formatos de medios físicos.
/// </summary>
public static class MediaFormatExtensions
{
    /// <summary>
    /// Deduce el formato físico a partir de descriptores textuales canónicos (Discogs, IA Vision, etc.).
    /// </summary>
    public static MediaFormat? ParseFromText(string? formatText)
    {
        if (string.IsNullOrWhiteSpace(formatText))
        {
            return null;
        }

        var normalized = formatText.Trim().ToLowerInvariant();

        if (normalized.Contains("cd") || normalized.Contains("compact disc") || normalized.Contains("sacd") || normalized.Contains("cdr"))
        {
            return MediaFormat.CD;
        }

        if (normalized.Contains("cassette") || normalized.Contains("cass") || normalized.Contains("tape") || normalized.Contains("c-60") || normalized.Contains("c-90"))
        {
            return MediaFormat.Cassette;
        }

        if (normalized.Contains("box") || normalized.Contains("boxset") || normalized.Contains("box set"))
        {
            return MediaFormat.BoxSet;
        }

        if (normalized.Contains("vinyl") || normalized.Contains("lp") || normalized.Contains("12\"") || normalized.Contains("7\"") || normalized.Contains("10\"") || normalized.Contains("single") || normalized.Contains("ep"))
        {
            return MediaFormat.Vinyl;
        }

        return null;
    }
}

