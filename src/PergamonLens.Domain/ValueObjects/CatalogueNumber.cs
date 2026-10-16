using System.Diagnostics.CodeAnalysis;
using PergamonLens.Domain.Exceptions;

namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Value Object inmutable que representa el número de catálogo asignado por el sello discográfico (ej. 'PCS 7027', 'SW-383').
/// Garantiza normalización en mayúsculas, recorte de espacios y límites de longitud.
/// </summary>
public sealed record CatalogueNumber
{
    public const int MaxLength = 60;

    public string Value { get; }

    private CatalogueNumber(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Crea una instancia validada de <see cref="CatalogueNumber"/>.
    /// Lanza <see cref="InvalidCatalogueNumberException"/> si el formato es inválido.
    /// </summary>
    public static CatalogueNumber Create(string? rawValue)
    {
        if (!TryCreate(rawValue, out CatalogueNumber? catalogueNumber, out string? error))
        {
            throw new InvalidCatalogueNumberException(error);
        }

        return catalogueNumber;
    }

    /// <summary>
    /// Intenta crear una instancia de <see cref="CatalogueNumber"/> sin lanzar excepciones.
    /// </summary>
    public static bool TryCreate(
        string? rawValue,
        [NotNullWhen(true)] out CatalogueNumber? catalogueNumber,
        [NotNullWhen(false)] out string? error)
    {
        catalogueNumber = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = "El número de catálogo no puede estar vacío.";
            return false;
        }

        string normalized = rawValue.Trim().ToUpperInvariant();

        if (normalized.Length > MaxLength)
        {
            error = $"El número de catálogo no puede superar los {MaxLength} caracteres.";
            return false;
        }

        catalogueNumber = new CatalogueNumber(normalized);
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(CatalogueNumber catalogueNumber) => catalogueNumber.Value;
}

