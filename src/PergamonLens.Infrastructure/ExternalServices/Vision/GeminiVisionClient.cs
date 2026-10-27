using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PergamonLens.Infrastructure.ExternalServices.Vision;

/// <summary>
/// Cliente HTTP para Google Gemini 2.0 Flash con Structured Outputs (JSON Schema estricto).
/// Soporta reconocimiento multimodal de música (vinilo/CD), libros (ISBN) y videojuegos (cartuchos/cajas).
/// </summary>
public sealed class GeminiVisionClient : IGeminiVisionClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<VisionOptions> _options;
    private readonly ILogger<GeminiVisionClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string SystemInstructionText = """
        Eres PergamonLens AI, un clasificador visual de élite especializado en coleccionables físicos:
        1. Música física (vinilos, CDs, casetes, boxsets).
        2. Libros (novelas, ensayos, cómics, libros de arte).
        3. Videojuegos retro y modernos (cartuchos, estuches de consola: PlayStation, Nintendo, Sega, Xbox, PC).

        Tu misión es analizar la fotografía (portada, contraportada, galleta/etiqueta central o lomo) y extraer los metadatos exactos:
        - mediaCategory: 'Music', 'Book', 'VideoGame' o 'Unknown'.
        - title: Título exacto de la obra, álbum, libro o videojuego.
        - creator: Artista/Grupo musical, Autor del libro, o Desarrollador/Estudio principal del videojuego.
        - format: Formato físico específico (ej: 'Vinyl LP', '7" Single', 'CD', 'Cassette', 'Hardcover', 'Paperback', 'Cartridge', 'Optical Disc', etc.).
        - platform: Si es videojuego, la consola o sistema (ej: 'PlayStation 5', 'Nintendo Switch', 'Super Nintendo', 'Mega Drive', 'Game Boy', 'PC'). Null si es música o libros.
        - releaseYear: Año de lanzamiento original o de la edición visible (ej: 1973).
        - publisherOrLabel: Sello discográfico, Editorial de libro o Publisher de videojuego.
        - catalogueOrIdentifier: Número de catálogo discográfico, ISBN-10/13 de libro o código de serie de videojuego (ej: CUSA-XXXX, SLUS-XXXX).
        - barcode: Código de barras numérico si es legible en la imagen.
        - confidence: Puntuación de certeza entre 0.0 y 1.0.
        - suggestedSearchQuery: Cadena canónica óptima para buscar en Discogs, OpenLibrary o IGDB (ej: 'Pink Floyd The Dark Side of the Moon 1973', 'Stephen King The Shining 1977', 'The Legend of Zelda Ocarina of Time Nintendo 64').

        Si la portada no tiene texto escrito pero es una obra famosa (reconocimiento iconográfico cultural, ej. el prisma de 'The Dark Side of the Moon', Led Zeppelin IV, o cartuchos clásicos), identifica la obra por su diseño gráfico.
        """;

    public GeminiVisionClient(
        HttpClient httpClient,
        IOptions<VisionOptions> options,
        ILogger<GeminiVisionClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<GeminiVisionAnalysisDto?> AnalyzeMediaCoverAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var apiKey = _options.Value.EffectiveApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API Key no configurada. Omitiendo llamada a Gemini 2.0 Flash.");
            return null;
        }

        try
        {
            // 1. Convertir la imagen a Base64
            byte[] imageBytes;
            if (imageStream is MemoryStream ms)
            {
                imageBytes = ms.ToArray();
            }
            else
            {
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream, cancellationToken);
                imageBytes = memoryStream.ToArray();
            }

            var base64Data = Convert.ToBase64String(imageBytes);

            // 2. Construir la solicitud con Structured Outputs (JSON Schema)
            var requestPayload = BuildGeminiRequestPayload(base64Data, contentType);
            var jsonContent = new StringContent(
                requestPayload.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            var endpoint = $"v1beta/models/{_options.Value.Model}:generateContent?key={apiKey}";

            _logger.LogInformation("Enviando petición de análisis visual multimodal a Google Gemini (Modelo: {Model}, Tamaño: {Bytes} bytes)...",
                _options.Value.Model, imageBytes.Length);

            using var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Error en respuesta de Gemini API. StatusCode: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseGeminiResponse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción durante la invocación del cliente de Gemini Vision.");
            return null;
        }
    }

    private static JsonObject BuildGeminiRequestPayload(string base64Data, string contentType)
    {
        // JSON Schema estricto para Structured Outputs
        var responseSchema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["mediaCategory"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["enum"] = new JsonArray("Music", "Book", "VideoGame", "Unknown"),
                    ["description"] = "Categoría del artículo físico."
                },
                ["title"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Título principal."
                },
                ["creator"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Artista/Banda (música), Autor (libros) o Desarrollador/Estudio (videojuegos)."
                },
                ["format"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Formato físico específico (Vinyl LP, CD, Hardcover, Cartridge, etc.)."
                },
                ["platform"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Consola o sistema en caso de videojuegos. Nulo en música o libros."
                },
                ["releaseYear"] = new JsonObject
                {
                    ["type"] = "INTEGER",
                    ["description"] = "Año de publicación o edición visible."
                },
                ["publisherOrLabel"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Sello discográfico, Editorial o Publisher."
                },
                ["catalogueOrIdentifier"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Número de catálogo, ISBN o Serial de juego."
                },
                ["barcode"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Código de barras numérico si es legible."
                },
                ["confidence"] = new JsonObject
                {
                    ["type"] = "NUMBER",
                    ["description"] = "Confianza del análisis entre 0.0 y 1.0."
                },
                ["suggestedSearchQuery"] = new JsonObject
                {
                    ["type"] = "STRING",
                    ["description"] = "Query de búsqueda canónica óptima."
                }
            },
            ["required"] = new JsonArray("mediaCategory", "title", "confidence", "suggestedSearchQuery")
        };

        return new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject { ["text"] = SystemInstructionText }
                }
            },
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["inline_data"] = new JsonObject
                            {
                                ["mime_type"] = contentType,
                                ["data"] = base64Data
                            }
                        },
                        new JsonObject
                        {
                            ["text"] = "Identifica este artículo físico coleccionable y extrae sus metadatos."
                        }
                    }
                }
            },
            ["generationConfig"] = new JsonObject
            {
                ["response_mime_type"] = "application/json",
                ["response_schema"] = responseSchema
            }
        };
    }

    private GeminiVisionAnalysisDto? ParseGeminiResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Gemini API no devolvió candidatos en la respuesta.");
                return null;
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                _logger.LogWarning("Respuesta de Gemini carece de partes de contenido válidas.");
                return null;
            }

            var rawJsonText = parts[0].GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(rawJsonText))
            {
                _logger.LogWarning("El texto JSON devuelto por Gemini está vacío.");
                return null;
            }

            _logger.LogInformation("JSON estructurado recibido de Gemini: {Json}", rawJsonText);

            return JsonSerializer.Deserialize<GeminiVisionAnalysisDto>(rawJsonText, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando la respuesta JSON estructurada de Gemini.");
            return null;
        }
    }
}

