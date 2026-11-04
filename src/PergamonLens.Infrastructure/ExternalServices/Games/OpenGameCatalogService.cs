using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PergamonLens.Domain.Services;

namespace PergamonLens.Infrastructure.ExternalServices.Games;

/// <summary>
/// Cliente HTTP para el catálogo abierto de videojuegos físicos (Consola, Retro y PC) con coste 0,00 € y sin API Key.
/// Combina MediaWiki Box-Art Search (exclusivos de PlayStation, Nintendo, Sega, Xbox y retro) con Steam Store Search.
/// </summary>
public sealed partial class OpenGameCatalogService : IGameCatalogService
{
    private static readonly string[] KnownPlatforms =
    [
        "PlayStation 5", "PS5", "PlayStation 4", "PS4", "PlayStation 3", "PS3", "PlayStation 2", "PS2", "PlayStation", "PS1",
        "Nintendo Switch", "Super Nintendo", "SNES", "Nintendo 64", "N64", "GameCube", "Wii U", "Wii", "Nintendo Entertainment System", "NES",
        "Game Boy Advance", "Game Boy Color", "Game Boy", "Nintendo DS", "Nintendo 3DS",
        "Sega Mega Drive", "Sega Genesis", "Dreamcast", "Sega Saturn", "Master System",
        "Xbox Series X", "Xbox One", "Xbox 360", "Xbox", "PC"
    ];

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenGameCatalogService> _logger;

    public OpenGameCatalogService(
        HttpClient httpClient,
        ILogger<OpenGameCatalogService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<OpenGameCatalogService>.Instance;
    }

    public async Task<IReadOnlyList<GameCatalogSearchResult>> SearchByQueryAsync(
        string title,
        string? platformOrStudio = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return [];
        }

        // Buscamos primero por el título limpio del videojuego para maximizar la precisión de la portada
        // tanto en MediaWiki como en Steam Store (añadir la consola/estudio contamina la búsqueda exacta).
        var results = await SearchByFreeTextAsync(title.Trim(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(platformOrStudio))
        {
            string? hintedPlatform = ExtractPlatform(platformOrStudio, platformOrStudio);
            if (!string.IsNullOrWhiteSpace(hintedPlatform))
            {
                results = results
                    .Select(r => string.IsNullOrWhiteSpace(r.Platform) || r.Platform == "Multiplataforma / PC"
                        ? r with { Platform = hintedPlatform }
                        : r)
                    .ToList();
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<GameCatalogSearchResult>> SearchByFreeTextAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        string cleanQuery = query.Trim();

        // Ejecutamos MediaWiki y Steam Store en paralelo para combinar metadatos de consola/retro y carátulas de alta resolución
        var wikiTask = SearchMediaWikiVideoGamesAsync(cleanQuery, cancellationToken);
        var steamTask = SearchSteamStoreAsync(cleanQuery, cancellationToken);

        await Task.WhenAll(wikiTask, steamTask);

        var wikiResults = await wikiTask;
        var steamResults = await steamTask;

        var combined = new List<GameCatalogSearchResult>();

        foreach (var w in wikiResults)
        {
            var item = w;
            if (string.IsNullOrWhiteSpace(item.CoverImageUrl))
            {
                var matchingSteam = steamResults.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.CoverImageUrl) &&
                    (s.Title.Equals(item.Title, StringComparison.OrdinalIgnoreCase) ||
                     s.Title.Contains(item.Title, StringComparison.OrdinalIgnoreCase) ||
                     item.Title.Contains(s.Title, StringComparison.OrdinalIgnoreCase)));

                if (matchingSteam is not null)
                {
                    item = item with { CoverImageUrl = matchingSteam.CoverImageUrl };
                }
            }

            combined.Add(item);
        }

        foreach (var s in steamResults)
        {
            if (!combined.Any(w => w.Title.Equals(s.Title, StringComparison.OrdinalIgnoreCase)))
            {
                combined.Add(s);
            }
        }

        return combined
            .OrderBy(r => string.IsNullOrWhiteSpace(r.CoverImageUrl) ? 1 : 0)
            .ToList();
    }

    private async Task<IReadOnlyList<GameCatalogSearchResult>> SearchMediaWikiVideoGamesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            string searchTerm = query.Contains("video game", StringComparison.OrdinalIgnoreCase)
                ? query
                : $"{query} video game";

            string url = $"https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrsearch={Uri.EscapeDataString(searchTerm)}&gsrlimit=8&prop=pageimages|extracts|description&pilicense=any&piprop=original|thumbnail&pithumbsize=500&exintro=1&explaintext=1&exsentences=2&format=json";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("query", out var queryEl) ||
                !queryEl.TryGetProperty("pages", out var pagesEl) ||
                pagesEl.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var results = new List<(int Index, GameCatalogSearchResult Item)>();

            foreach (var pageProp in pagesEl.EnumerateObject())
            {
                var page = pageProp.Value;
                string? rawTitle = page.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(rawTitle))
                {
                    continue;
                }

                string extract = page.TryGetProperty("extract", out var exEl) && exEl.ValueKind == JsonValueKind.String
                    ? exEl.GetString() ?? string.Empty
                    : string.Empty;

                string description = page.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() ?? string.Empty
                    : string.Empty;

                string combinedText = $"{description}. {extract}";
                if (!IsLikelyVideoGameArticle(rawTitle, combinedText))
                {
                    continue;
                }

                string cleanTitle = CleanWikiTitle(rawTitle);
                int index = page.TryGetProperty("index", out var idxEl) && idxEl.TryGetInt32(out var idxVal) ? idxVal : 99;
                long pageId = page.TryGetProperty("pageid", out var idEl) && idEl.TryGetInt64(out var idVal) ? idVal : 0;

                string? coverUrl = null;
                if (page.TryGetProperty("thumbnail", out var thumbEl) &&
                    thumbEl.TryGetProperty("source", out var thumbSrc) &&
                    thumbSrc.ValueKind == JsonValueKind.String)
                {
                    coverUrl = thumbSrc.GetString();
                }
                else if (page.TryGetProperty("original", out var origEl) &&
                         origEl.TryGetProperty("source", out var origSrc) &&
                         origSrc.ValueKind == JsonValueKind.String)
                {
                    coverUrl = origSrc.GetString();
                }

                int? releaseYear = ExtractYear(combinedText);
                string developer = ExtractDeveloper(combinedText) ?? "Estudio Oficial";
                string? publisher = ExtractPublisher(combinedText);
                string? platform = ExtractPlatform(combinedText, query);

                var item = new GameCatalogSearchResult(
                    GameKey: $"WIKI-{pageId}",
                    Title: cleanTitle,
                    Developer: developer,
                    ReleaseYear: releaseYear,
                    Publisher: publisher,
                    Platform: platform,
                    CoverImageUrl: coverUrl);

                results.Add((index, item));
            }

            return results
                .OrderBy(r => r.Item.CoverImageUrl is null ? 1 : 0)
                .ThenBy(r => r.Index)
                .Select(r => r.Item)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al consultar MediaWiki API para videojuegos ({Query})", query);
            return [];
        }
    }

    private async Task<IReadOnlyList<GameCatalogSearchResult>> SearchSteamStoreAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            string url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(query)}&l=english&cc=US";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<GameCatalogSearchResult>();
            foreach (var item in itemsEl.EnumerateArray().Take(6))
            {
                string? name = item.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                long appId = item.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var idVal) ? idVal : 0;

                if (string.IsNullOrWhiteSpace(name) || appId <= 0)
                {
                    continue;
                }

                // Carátula vertical oficial de caja (600x900) desde el CDN de Steam
                string coverUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appId}/library_600x900.jpg";

                list.Add(new GameCatalogSearchResult(
                    GameKey: $"STEAM-{appId}",
                    Title: name.Trim(),
                    Developer: "Edición Multiplataforma / PC",
                    ReleaseYear: null,
                    Publisher: "Steam / Oficial",
                    Platform: ExtractPlatform(query, query) ?? "Multiplataforma / PC",
                    CoverImageUrl: coverUrl));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al consultar Steam Store API ({Query})", query);
            return [];
        }
    }

    private static bool IsLikelyVideoGameArticle(string title, string text)
    {
        if (title.StartsWith("List of ", StringComparison.OrdinalIgnoreCase) ||
            title.StartsWith("Category:", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("soundtrack", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("disambiguation", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("film)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("TV series)", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("(series)", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return text.Contains("video game", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("game developed", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("action-adventure", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("role-playing", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("platform game", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("fighting game", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("racing game", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("shooter", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("(video game)", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanWikiTitle(string rawTitle)
    {
        return WikiSuffixRegex().Replace(rawTitle, string.Empty).Trim();
    }

    private static int? ExtractYear(string text)
    {
        var match = YearRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int year))
        {
            return year;
        }
        return null;
    }

    private static string? ExtractDeveloper(string text)
    {
        var match = DeveloperRegex().Match(text);
        if (match.Success)
        {
            return CleanExtractedEntity(match.Groups[1].Value);
        }
        return null;
    }

    private static string? ExtractPublisher(string text)
    {
        var match = PublisherRegex().Match(text);
        if (match.Success)
        {
            return CleanExtractedEntity(match.Groups[1].Value);
        }
        return null;
    }

    private static string? ExtractPlatform(string text, string query)
    {
        foreach (var p in KnownPlatforms)
        {
            if (query.Contains(p, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        foreach (var p in KnownPlatforms)
        {
            if (text.Contains(p, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return null;
    }

    private static string CleanExtractedEntity(string raw)
    {
        string cleaned = raw.Trim();
        int andIdx = cleaned.IndexOf(" and published", StringComparison.OrdinalIgnoreCase);
        if (andIdx > 0)
        {
            cleaned = cleaned[..andIdx];
        }
        int forIdx = cleaned.IndexOf(" for the ", StringComparison.OrdinalIgnoreCase);
        if (forIdx > 0)
        {
            cleaned = cleaned[..forIdx];
        }
        int inIdx = cleaned.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (inIdx > 0)
        {
            cleaned = cleaned[..inIdx];
        }
        return cleaned.Trim(' ', '.', ',', ';');
    }

    [GeneratedRegex(@"\s*\((?:\d{4}\s+)?video game\)$", RegexOptions.IgnoreCase)]
    private static partial Regex WikiSuffixRegex();

    [GeneratedRegex(@"\b(19[7-9]\d|20[0-2]\d)\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"developed by\s+([^.,;()]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DeveloperRegex();

    [GeneratedRegex(@"published by\s+([^.,;()]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PublisherRegex();
}

