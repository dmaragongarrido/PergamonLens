namespace PergamonLens.Domain.Enums;

/// <summary>
/// Soporte o formato físico del ejemplar coleccionable (Música, Libros o Videojuegos).
/// </summary>
public enum MediaFormat
{
    // --- Música (1..9) ---
    Vinyl = 1,              // Disco de vinilo (LP, Single, Maxi, EP)
    CD = 2,                 // Compact Disc (Jewel Case, Digipak, Mini-LP CD)
    Cassette = 3,           // Casete de audio (Tape)
    BoxSet = 4,             // Caja recopilatoria o edición especial multiformato

    // --- Libros (10..19) ---
    Hardcover = 10,         // Tapa dura / Cartoné
    Paperback = 11,         // Rústica / Bolsillo / Tapa blanda
    ComicManga = 12,        // Cómic / Novela gráfica / Manga / Tomo
    BookSpecialEdition = 13,// Edición especial / Coleccionista / Facsímil

    // --- Videojuegos (20..29) ---
    OpticalDiscGame = 20,   // Disco óptico en caja (Blu-ray, DVD, CD-ROM, GD-ROM, UMD)
    Cartridge = 21,         // Cartucho o tarjeta física (Switch, SNES, Mega Drive, Game Boy, N64)
    GameCollectorEdition = 22 // Edición Coleccionista / Big Box
}
