using System.Diagnostics.CodeAnalysis;
using PergamonLens.Domain.Exceptions;

namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Value Object inmutable que representa un código de barras estándar (EAN-13, UPC-A, EAN-8).
/// Garantiza la normalización y validación del dígito de control (algoritmo estándar GS1).
/// </summary>
public sealed record Barcode
{
    public string Value { get; }

    private Barcode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Crea una instancia validada de <see cref="Barcode"/>.
    /// Lanza <see cref="InvalidBarcodeException"/> si el formato o dígito de control son inválidos.
    /// </summary>
    public static Barcode Create(string? rawValue)
    {
        if (!TryCreate(rawValue, out Barcode? barcode, out string? error))
        {
            throw new InvalidBarcodeException(error);
        }

        return barcode;
    }

    /// <summary>
    /// Intenta crear una instancia de <see cref="Barcode"/> sin lanzar excepciones.
    /// </summary>
    public static bool TryCreate(
        string? rawValue,
        [NotNullWhen(true)] out Barcode? barcode,
        [NotNullWhen(false)] out string? error)
    {
        barcode = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = "El código de barras no puede estar vacío.";
            return false;
        }

        // Normalizar eliminando espacios en blanco y guiones habituales en empaquetados físicos
        string normalized = rawValue.Replace(" ", string.Empty).Replace("-", string.Empty);

        if (!normalized.All(char.IsAsciiDigit))
        {
            error = $"El código de barras '{rawValue}' contiene caracteres no numéricos.";
            return false;
        }

        if (normalized.Length is not (8 or 12 or 13))
        {
            error = $"La longitud del código de barras '{normalized.Length}' es inválida. Debe ser EAN-8 (8), UPC-A (12) o EAN-13 (13).";
            return false;
        }

        if (!IsValidGs1Checksum(normalized))
        {
            error = $"El código de barras '{normalized}' tiene un dígito de control GS1 inválido.";
            return false;
        }

        barcode = new Barcode(normalized);
        return true;
    }

    /// <summary>
    /// Valida el dígito de control siguiendo la especificación estándar GS1.
    /// </summary>
    private static bool IsValidGs1Checksum(string digits)
    {
        int checkDigit = digits[^1] - '0';
        int sum = 0;

        // Ponderación de derecha a izquierda: el penúltimo dígito se multiplica por 3, el antepenúltimo por 1, etc.
        bool multiplyByThree = true;
        for (int i = digits.Length - 2; i >= 0; i--)
        {
            int digit = digits[i] - '0';
            sum += digit * (multiplyByThree ? 3 : 1);
            multiplyByThree = !multiplyByThree;
        }

        int calculatedCheckDigit = (10 - (sum % 10)) % 10;
        return calculatedCheckDigit == checkDigit;
    }

    public override string ToString() => Value;

    public static implicit operator string(Barcode barcode) => barcode.Value;
}

