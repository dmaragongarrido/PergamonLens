namespace PergamonLens.Domain.Enums;

/// <summary>
/// Estado del ciclo de vida del procesamiento inteligente de un vinilo en PergamonLens.
/// </summary>
public enum ProcessingStatus
{
    PendingAnalysis = 1, // Fotografía subida, esperando procesamiento por IA multimodal
    Identified = 2,      // La IA extrajo título, artista, código de barras o número de catálogo
    Enriched = 3,        // Datos contrastados y enriquecidos contra APIs canónicas (Discogs)
    Confirmed = 4,       // Verificado y custodiado por el coleccionista en su bóveda personal
    Failed = 5           // Error en el análisis de imagen o ítem no reconocible
}

