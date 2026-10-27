using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.ExternalServices.Discogs.Models;

namespace PergamonLens.Infrastructure.ExternalServices.Discogs;

/// <summary>
/// Cliente HTTP para la API canónica de Discogs con gestión de resiliencia y mapeo de contratos de dominio.
/// </summary>
public sealed class DiscogsService : IDiscogsService
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
            string? format = response.Formats?.FirstOrDefault()?.Name;

            var tracks = (response.Tracklist ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .Select(t => new DiscogsTrackItem(
                    string.IsNullOrWhiteSpace(t.Position) ? "-" : t.Position.Trim(),
                    t.Title.Trim(),
                    ParseDuration(t.Duration)))
                .ToList();

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
                format);
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

                    return new DiscogsSearchResult(
                        r.Id,
                        title,
                        artist,
                        year,
                        r.Label?.FirstOrDefault(),
                        r.Catno,
                        r.Country,
                        r.Thumb,
                        format);
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al buscar en Discogs con la URL: {Url}", url);
            return [];
        }
    }

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
}
