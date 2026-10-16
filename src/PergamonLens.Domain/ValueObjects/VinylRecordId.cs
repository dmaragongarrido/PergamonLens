namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Identificador fuertemente tipado para el agregado <see cref="PergamonLens.Domain.Entities.VinylRecord"/>.
/// </summary>
public readonly record struct VinylRecordId(Guid Value)
{
    public static VinylRecordId New() => new(Guid.NewGuid());

    public static VinylRecordId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(VinylRecordId id) => id.Value;

    public static explicit operator VinylRecordId(Guid value) => new(value);
}

