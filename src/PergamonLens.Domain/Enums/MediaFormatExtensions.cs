namespace PergamonLens.Domain.Enums;

/// <summary>
/// Extensiones y utilidades para la deducción e inferencia de formatos y categorías de medios físicos.
/// </summary>
public static class MediaFormatExtensions
{
    /// <summary>
    /// Determina la categoría principal a partir del formato físico del soporte.
    /// </summary>
    public static MediaCategory GetCategory(this MediaFormat format) => format switch
    {
        MediaFormat.Hardcover or MediaFormat.Paperback or MediaFormat.ComicManga or MediaFormat.BookSpecialEdition
            => MediaCategory.Books,
        MediaFormat.OpticalDiscGame or MediaFormat.Cartridge or MediaFormat.GameCollectorEdition
            => MediaCategory.VideoGames,
        _ => MediaCategory.Music
    };

    /// <summary>
    /// Determina si un código de barras corresponde a un ISBN-13 (prefijos Bookland 978 o 979) o ISBN-10.
    /// </summary>
    public static bool IsIsbn(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return false;
        }

        var digits = new string(rawCode.Where(char.IsLetterOrDigit).ToArray());
        if (digits.Length == 13 && (digits.StartsWith("978", StringComparison.Ordinal) || digits.StartsWith("979", StringComparison.Ordinal)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Deduce el formato físico a partir de descriptores textuales canónicos (Discogs, OpenLibrary, IA Vision, etc.).
    /// </summary>
    public static MediaFormat? ParseFromText(string? formatText)
    {
        if (string.IsNullOrWhiteSpace(formatText))
        {
            return null;
        }

        var normalized = formatText.Trim().ToLowerInvariant();

        // --- Formatos de Libros ---
        if (normalized.Contains("hardcover") || normalized.Contains("tapa dura") || normalized.Contains("cartoné") || normalized.Contains("cartone") || normalized.Contains("hardback"))
        {
            return MediaFormat.Hardcover;
        }

        if (normalized.Contains("paperback") || normalized.Contains("rústica") || normalized.Contains("rustica") || normalized.Contains("bolsillo") || normalized.Contains("softcover") || normalized.Contains("tapa blanda"))
        {
            return MediaFormat.Paperback;
        }

        if (normalized.Contains("comic") || normalized.Contains("cómic") || normalized.Contains("manga") || normalized.Contains("graphic novel") || normalized.Contains("novela gráfica"))
        {
            return MediaFormat.ComicManga;
        }

        if (normalized.Contains("bookspecialedition") || normalized.Contains("edición especial libro") || normalized.Contains("facsímil"))
        {
            return MediaFormat.BookSpecialEdition;
        }

        // --- Formatos de Videojuegos ---
        if (normalized.Contains("cartridge") || normalized.Contains("cartucho") || normalized.Contains("game card") || normalized.Contains("tarjeta switch"))
        {
            return MediaFormat.Cartridge;
        }

        if (normalized.Contains("opticaldiscgame") || normalized.Contains("blu-ray game") || normalized.Contains("dvd-rom") || normalized.Contains("gd-rom") || normalized.Contains("umd"))
        {
            return MediaFormat.OpticalDiscGame;
        }

        if (normalized.Contains("gamecollectoredition") || normalized.Contains("big box"))
        {
            return MediaFormat.GameCollectorEdition;
        }

        // --- Formatos de Música ---
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

        if (normalized.Contains("book") || normalized.Contains("libro"))
        {
            return MediaFormat.Paperback;
        }

        return null;
    }
}
