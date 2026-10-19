namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Identificador fuertemente tipado para el agregado <see cref="PergamonLens.Domain.Entities.MusicRelease"/>.
/// </summary>
public readonly record struct MusicReleaseId(Guid Value)
{
    public static MusicReleaseId New() => new(Guid.NewGuid());

    public static MusicReleaseId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(MusicReleaseId id) => id.Value;

    public static explicit operator MusicReleaseId(Guid value) => new(value);
}

