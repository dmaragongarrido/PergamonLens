using System.Text.Json.Serialization;

namespace PergamonLens.Infrastructure.ExternalServices.Discogs.Models;

public sealed class DiscogsSearchApiResponse
{
    [JsonPropertyName("results")]
    public List<DiscogsSearchItemResponse> Results { get; set; } = [];
}

public sealed class DiscogsSearchItemResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("label")]
    public List<string>? Label { get; set; }

    [JsonPropertyName("catno")]
    public string? Catno { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("thumb")]
    public string? Thumb { get; set; }

    [JsonPropertyName("format")]
    public List<string>? Format { get; set; }
}

public sealed class DiscogsReleaseApiResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artists")]
    public List<DiscogsArtistResponse>? Artists { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("labels")]
    public List<DiscogsLabelResponse>? Labels { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("images")]
    public List<DiscogsImageResponse>? Images { get; set; }

    [JsonPropertyName("tracklist")]
    public List<DiscogsTrackResponse>? Tracklist { get; set; }

    [JsonPropertyName("formats")]
    public List<DiscogsFormatResponse>? Formats { get; set; }
}

public sealed class DiscogsFormatResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("qty")]
    public string? Qty { get; set; }

    [JsonPropertyName("descriptions")]
    public List<string>? Descriptions { get; set; }
}

public sealed class DiscogsArtistResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class DiscogsLabelResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("catno")]
    public string? Catno { get; set; }
}

public sealed class DiscogsImageResponse
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

public sealed class DiscogsTrackResponse
{
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
}
