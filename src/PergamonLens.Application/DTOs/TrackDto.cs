namespace PergamonLens.Application.DTOs;

/// <summary>
/// DTO que representa una pista musical individual.
/// </summary>
public sealed record TrackDto(
    Guid Id,
    string Position,
    string Title,
    TimeSpan? Duration,
    string? FormattedDuration);

