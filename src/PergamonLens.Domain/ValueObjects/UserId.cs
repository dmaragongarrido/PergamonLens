using System.Diagnostics.CodeAnalysis;
using PergamonLens.Domain.Exceptions;

namespace PergamonLens.Domain.ValueObjects;

/// <summary>
/// Value Object inmutable que representa el identificador del usuario propietario de una colección física.
/// Encapsula la identidad y garantiza el aislamiento de datos entre coleccionistas (Multi-tenancy / Anti-BOLA).
/// </summary>
public sealed record UserId
{
    public string Value { get; }

    private UserId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Genera un nuevo identificador único de usuario basado en GUID.
    /// </summary>
    public static UserId New() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Crea una instancia validada de <see cref="UserId"/> a partir de una cadena.
    /// </summary>
    public static UserId From(string? rawValue)
    {
        if (!TryCreate(rawValue, out var userId, out var error))
        {
            throw new InvalidUserIdException(error);
        }

        return userId;
    }

    /// <summary>
    /// Intenta crear una instancia de <see cref="UserId"/> a partir de una cadena, retornando null si es inválida o vacía.
    /// </summary>
    public static UserId? TryFrom(string? rawValue) =>
        TryCreate(rawValue, out var userId, out _) ? userId : null;

    /// <summary>
    /// Intenta crear una instancia de <see cref="UserId"/> sin lanzar excepciones.
    /// </summary>
    public static bool TryCreate(
        string? rawValue,
        [NotNullWhen(true)] out UserId? userId,
        [NotNullWhen(false)] out string? error)
    {
        userId = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = "El identificador del usuario (UserId) no puede estar vacío.";
            return false;
        }

        string trimmed = rawValue.Trim();

        if (trimmed.Length > 450)
        {
            error = "El identificador del usuario no puede superar los 450 caracteres.";
            return false;
        }

        userId = new UserId(trimmed);
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(UserId userId) => userId.Value;
}
