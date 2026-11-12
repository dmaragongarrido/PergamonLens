using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.ExternalServices.Discogs.Models;

namespace PergamonLens.Infrastructure.ExternalServices.Discogs;

/// <summary>
/// Cliente HTTP para la API canónica de Discogs con gestión de resiliencia y mapeo de contratos de dominio.
/// </summary>
public sealed partial class DiscogsService : IDiscogsService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<DiscogsOptions> _options;
    private readonly ILogger<DiscogsService> _logger;

    public DiscogsService(
        HttpClient httpClient,
        IOptions<DiscogsOptions> options,
        ILogger<DiscogsService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscogsSearchResult>> SearchByBarcodeAsync(
        Barcode barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        string url = $"database/search?barcode={Uri.EscapeDataString(barcode.Value)}&type=release";
        return await ExecuteSearchAsync(url, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscogsSearchResult>> SearchByCatalogueNumberAsync(
        CatalogueNumber catalogueNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalogueNumber);
        string url = $"database/search?catno={Uri.EscapeDataString(catalogueNumber.Value)}&type=release";
        return await ExecuteSearchAsync(url, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscogsSearchResult>> SearchByQueryAsync(
        string artist,
        string title,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artist);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        string url = $"database/search?artist={Uri.EscapeDataString(artist)}&release_title={Uri.EscapeDataString(title)}&type=release";
        return await ExecuteSearchAsync(url, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscogsSearchResult>> SearchByFreeTextAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        string url = $"database/search?q={Uri.EscapeDataString(query)}&type=release";
        return await ExecuteSearchAsync(url, cancellationToken);
    }

    public async Task<DiscogsReleaseDetails?> GetReleaseDetailsAsync(
        long releaseId,
        CancellationToken cancellationToken = default)
    {
        if (releaseId <= 0)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<DiscogsReleaseApiResponse>(
                $"releases/{releaseId}",
                cancellationToken);

            if (response is null)
            {
                return null;
            }

            string artist = response.Artists?.FirstOrDefault()?.Name ?? "Varios Artistas";
            string? label = response.Labels?.FirstOrDefault()?.Name;
            string? catNo = response.Labels?.FirstOrDefault()?.Catno;
            string? coverImageUrl = response.Images?.FirstOrDefault()?.Uri;
            string? backCoverImageUrl = response.Images?.Skip(1).FirstOrDefault()?.Uri;
            string? format = response.Formats?.FirstOrDefault()?.Name;
            string? genre = FormatGenreAndStyle(response.Genres, response.Styles);
            string? overview = FormatDiscogsOverview(
                response.Notes,
                response.Title,
                artist,
                response.Year,
                label,
                response.Country,
                genre,
                response.Tracklist?.Count ?? 0);

            var flattenedTracks = FlattenTracks(response.Tracklist)
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .ToList();

            var usedPositions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tracks = new List<DiscogsTrackItem>();
            int fallbackIndex = 1;

            foreach (var t in flattenedTracks)
            {
                string basePos = string.IsNullOrWhiteSpace(t.Position)
                    ? (fallbackIndex++).ToString()
                    : t.Position.Trim();

                string candidate = basePos;
                int suffix = 2;
                while (usedPositions.Contains(candidate))
                {
                    candidate = $"{basePos}.{suffix++}";
                }

                usedPositions.Add(candidate);
                tracks.Add(new DiscogsTrackItem(
                    candidate,
                    t.Title.Trim(),
                    ParseDuration(t.Duration)));
            }

            return new DiscogsReleaseDetails(
                response.Id,
                response.Title.Trim(),
                artist.Trim(),
                response.Year,
                label,
                catNo,
                response.Country,
                coverImageUrl,
                tracks,
                format,
                backCoverImageUrl,
                genre,
                overview);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al consultar la release {ReleaseId} en la API de Discogs", releaseId);
            return null;
        }
    }

    private async Task<IReadOnlyList<DiscogsSearchResult>> ExecuteSearchAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DiscogsSearchApiResponse>(url, cancellationToken);
            if (response?.Results is null || response.Results.Count == 0)
            {
                return [];
            }

            return response.Results
                .Select(r =>
                {
                    var (artist, title) = SplitArtistAndTitle(r.Title);
                    int? year = int.TryParse(r.Year, out int y) ? y : null;
                    string? format = r.Format?.FirstOrDefault();
                    string? genre = FormatGenreAndStyle(r.Genre, r.Style);

                    return new DiscogsSearchResult(
                        r.Id,
                        title,
                        artist,
                        year,
                        r.Label?.FirstOrDefault(),
                        r.Catno,
                        r.Country,
                        r.Thumb,
                        format,
                        genre);
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al buscar en Discogs con la URL: {Url}", url);
            return [];
        }
    }

    private static string? FormatGenreAndStyle(List<string>? genres, List<string>? styles)
    {
        var primaryStyle = styles?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
        var primaryGenre = genres?.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g))?.Trim();

        string? result;
        if (!string.IsNullOrWhiteSpace(primaryStyle) && !string.IsNullOrWhiteSpace(primaryGenre) &&
            !primaryStyle.Equals(primaryGenre, StringComparison.OrdinalIgnoreCase))
        {
            result = $"{primaryGenre} / {primaryStyle}";
        }
        else
        {
            result = primaryStyle ?? primaryGenre;
        }

        return NormalizeMusicGenre(result);
    }

    private static string? NormalizeMusicGenre(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var normalized = raw
            .Replace("Classical", "Clásica", StringComparison.OrdinalIgnoreCase)
            .Replace("Electronic", "Electrónica", StringComparison.OrdinalIgnoreCase)
            .Replace("Stage & Screen", "Banda Sonora", StringComparison.OrdinalIgnoreCase)
            .Replace("Soundtrack", "Banda Sonora", StringComparison.OrdinalIgnoreCase)
            .Replace("Folk, World, & Country", "Folk / Tradicional", StringComparison.OrdinalIgnoreCase);

        return normalized.Trim();
    }

    private static string? FormatDiscogsOverview(
        string? rawNotes,
        string title,
        string artist,
        int? year,
        string? label,
        string? country,
        string? genre,
        int trackCount)
    {
        if (!string.IsNullOrWhiteSpace(rawNotes))
        {
            var cleaned = DiscogsBbCodeRegex().Replace(rawNotes, "$1");
            var lines = cleaned
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.Length >= 25 &&
                               !line.Contains("Matrix", StringComparison.OrdinalIgnoreCase) &&
                               !line.Contains("Barcode", StringComparison.OrdinalIgnoreCase) &&
                               !line.Contains("Rights Society", StringComparison.OrdinalIgnoreCase) &&
                               !line.Contains("http", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var candidate = lines.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                if (candidate.Length > 220)
                {
                    candidate = candidate[..217].TrimEnd() + "...";
                }
                return candidate;
            }
        }

        return null;
    }

    [GeneratedRegex(@"\[(?:a|l|r|m|url|b|i|u)=?([^\]]*)\]", RegexOptions.Compiled)]
    private static partial Regex DiscogsBbCodeRegex();

    private static (string Artist, string Title) SplitArtistAndTitle(string fullTitle)
    {
        if (string.IsNullOrWhiteSpace(fullTitle))
        {
            return ("Desconocido", "Sin Título");
        }

        var parts = fullTitle.Split(" - ", 2, StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            2 => (parts[0], parts[1]),
            _ => ("Desconocido", fullTitle.Trim())
        };
    }

    private static TimeSpan? ParseDuration(string? durationStr)
    {
        if (string.IsNullOrWhiteSpace(durationStr))
        {
            return null;
        }

        var parts = durationStr.Trim().Split(':');
        return parts.Length switch
        {
            2 when int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int sec) =>
                new TimeSpan(0, min, sec),
            3 when int.TryParse(parts[0], out int hr) && int.TryParse(parts[1], out int min) && int.TryParse(parts[2], out int sec) =>
                new TimeSpan(hr, min, sec),
            _ => null
        };
    }

    private static IEnumerable<DiscogsTrackResponse> FlattenTracks(IEnumerable<DiscogsTrackResponse>? tracklist)
    {
        if (tracklist is null)
        {
            yield break;
        }

        foreach (var track in tracklist)
        {
            if (string.Equals(track.Type, "heading", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (track.SubTracks is { Count: > 0 })
            {
                foreach (var subTrack in FlattenTracks(track.SubTracks))
                {
                    yield return subTrack;
                }
            }
            else
            {
                yield return track;
            }
        }
    }
}
