using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PergamonLens.Domain.Services;

namespace PergamonLens.Infrastructure.ExternalServices.OpenLibrary;

/// <summary>
/// Cliente HTTP para la API pública y gratuita de OpenLibrary (Internet Archive).
/// Permite buscar y enriquecer libros por ISBN-10/13, Autor + Título o texto libre con coste 0,00 €.
/// </summary>
public sealed class OpenLibraryService : IBookCatalogService
{
    private const string FieldsList = "key,title,author_name,first_publish_year,publisher,isbn,number_of_pages_median,cover_i,edition_key";

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenLibraryService> _logger;

    public OpenLibraryService(
        HttpClient httpClient,
        ILogger<OpenLibraryService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<OpenLibraryService>.Instance;
    }

    public async Task<BookCatalogSearchResult?> LookupByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        string cleanIsbn = new string(isbn.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(cleanIsbn))
        {
            return null;
        }

        var url = $"search.json?isbn={Uri.EscapeDataString(cleanIsbn)}&limit=5&fields={FieldsList}";
        var results = await ExecuteSearchAsync(url, cleanIsbn, cancellationToken);
        return results.FirstOrDefault();
    }

    public async Task<IReadOnlyList<BookCatalogSearchResult>> SearchByQueryAsync(
        string author,
        string title,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(author))
        {
            return [];
        }

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            queryParts.Add($"title={Uri.EscapeDataString(title.Trim())}");
        }
        if (!string.IsNullOrWhiteSpace(author) &&
            !author.Equals("Autor desconocido", StringComparison.OrdinalIgnoreCase) &&
            !author.Equals("Artista desconocido", StringComparison.OrdinalIgnoreCase))
        {
            queryParts.Add($"author={Uri.EscapeDataString(author.Trim())}");
        }

        queryParts.Add("limit=12");
        queryParts.Add($"fields={FieldsList}");

        var url = $"search.json?{string.Join("&", queryParts)}";
        var results = await ExecuteSearchAsync(url, null, cancellationToken);

        if (results.Count == 0 && !string.IsNullOrWhiteSpace(title))
        {
            return await SearchByFreeTextAsync($"{title} {author}".Trim(), cancellationToken);
        }

        return results;
    }

    public async Task<IReadOnlyList<BookCatalogSearchResult>> SearchByFreeTextAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var url = $"search.json?q={Uri.EscapeDataString(query.Trim())}&limit=12&fields={FieldsList}";
        return await ExecuteSearchAsync(url, null, cancellationToken);
    }

    private async Task<IReadOnlyList<BookCatalogSearchResult>> ExecuteSearchAsync(
        string relativeUrl,
        string? preferredIsbn,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenLibrary API devolvió estado {StatusCode} para URL {Url}", response.StatusCode, relativeUrl);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("docs", out var docsElement) || docsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<BookCatalogSearchResult>();

            foreach (var item in docsElement.EnumerateArray())
            {
                string? title = item.TryGetProperty("title", out var tProp) && tProp.ValueKind == JsonValueKind.String
                    ? tProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                string author = ExtractFirstStringFromArray(item, "author_name") ?? "Autor desconocido";
                string? publisher = ExtractFirstStringFromArray(item, "publisher");
                string? editionKey = ExtractFirstStringFromArray(item, "edition_key")
                    ?? (item.TryGetProperty("key", out var kProp) ? kProp.GetString()?.Replace("/works/", string.Empty) : null)
                    ?? Guid.NewGuid().ToString("N")[..10];

                int? publishYear = item.TryGetProperty("first_publish_year", out var yProp) && yProp.TryGetInt32(out var yVal)
                    ? yVal
                    : null;

                int? pagesCount = item.TryGetProperty("number_of_pages_median", out var pProp) && pProp.TryGetInt32(out var pVal) && pVal > 0
                    ? pVal
                    : null;

                string? isbn = preferredIsbn ?? ExtractPreferredIsbn(item);

                string? coverUrl = null;
                if (item.TryGetProperty("cover_i", out var cProp) && cProp.TryGetInt64(out var coverId) && coverId > 0)
                {
                    coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg";
                }
                else if (!string.IsNullOrWhiteSpace(isbn))
                {
                    coverUrl = $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg";
                }

                list.Add(new BookCatalogSearchResult(
                    EditionKey: editionKey!,
                    Title: title.Trim(),
                    Author: author.Trim(),
                    PublishYear: publishYear,
                    Publisher: publisher?.Trim(),
                    Isbn: isbn,
                    PagesCount: pagesCount,
                    CoverImageUrl: coverUrl));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al consultar OpenLibrary API ({Url})", relativeUrl);
            return [];
        }
    }

    private static string? ExtractFirstStringFromArray(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                {
                    return item.GetString();
                }
            }
        }
        return null;
    }

    private static string? ExtractPreferredIsbn(JsonElement element)
    {
        if (!element.TryGetProperty("isbn", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? firstIsbn = null;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var val = item.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(val))
            {
                continue;
            }

            firstIsbn ??= val;
            if (val.Length == 13 && (val.StartsWith("978", StringComparison.Ordinal) || val.StartsWith("979", StringComparison.Ordinal)))
            {
                return val;
            }
        }

        return firstIsbn;
    }
}

