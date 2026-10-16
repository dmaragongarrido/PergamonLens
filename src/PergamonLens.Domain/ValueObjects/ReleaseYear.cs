using System.Diagnostics.CodeAnalysis;
using PergamonLens.Domain.Exceptions;

namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Value Object inmutable que representa el año de lanzamiento de una publicación física o fonográfica.
/// Garantiza un rango temporal coherente con la historia del audio grabado (1877 - AñoActual + 1).
/// </summary>
public sealed record ReleaseYear
{
    public const int MinYear = 1877; // Invención del fonógrafo por Thomas Edison

    public int Value { get; }

    private ReleaseYear(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Crea una instancia validada de <see cref="ReleaseYear"/>.
    /// Lanza <see cref="InvalidReleaseYearException"/> si el año está fuera del rango permitido.
    /// </summary>
    public static ReleaseYear Create(int year)
    {
        if (!TryCreate(year, out ReleaseYear? releaseYear, out string? error))
        {
            throw new InvalidReleaseYearException(error);
        }

        return releaseYear;
    }

    /// <summary>
    /// Intenta crear una instancia de <see cref="ReleaseYear"/> sin lanzar excepciones.
    /// </summary>
    public static bool TryCreate(
        int year,
        [NotNullWhen(true)] out ReleaseYear? releaseYear,
        [NotNullWhen(false)] out string? error)
    {
        releaseYear = null;
        error = null;

        int maxYear = DateTime.UtcNow.Year + 1;

        if (year < MinYear || year > maxYear)
        {
            error = $"El año de lanzamiento '{year}' es inválido. Debe estar comprendido entre {MinYear} y {maxYear}.";
            return false;
        }

        releaseYear = new ReleaseYear(year);
        return true;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(ReleaseYear releaseYear) => releaseYear.Value;
}

