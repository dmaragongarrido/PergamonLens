using PergamonLens.Domain.Common;

namespace PergamonLens.Domain.Entities;

/// <summary>
/// Representa una pista o corte sonoro individual perteneciente a un disco de vinilo.
/// </summary>
public sealed class Track : Entity<Guid>
{
    public string Position { get; private set; } = string.Empty; // ej. "A1", "A2", "B1"
    public string Title { get; private set; } = string.Empty;
    public TimeSpan? Duration { get; private set; }

    private Track(Guid id, string position, string title, TimeSpan? duration) : base(id)
    {
        Position = position;
        Title = title;
        Duration = duration;
    }

    // Requerido por EF Core
    private Track() { }

    public static Track Create(string position, string title, TimeSpan? duration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(position, nameof(position));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        return new Track(Guid.NewGuid(), position.Trim().ToUpperInvariant(), title.Trim(), duration);
    }
}

