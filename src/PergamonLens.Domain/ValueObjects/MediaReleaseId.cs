namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Identificador fuertemente tipado para el agregado <see cref="PergamonLens.Domain.Entities.MediaRelease"/>.
/// </summary>
public readonly record struct MediaReleaseId(Guid Value)
{
    public static MediaReleaseId New() => new(Guid.NewGuid());

    public static MediaReleaseId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(MediaReleaseId id) => id.Value;

    public static explicit operator MediaReleaseId(Guid value) => new(value);
}
