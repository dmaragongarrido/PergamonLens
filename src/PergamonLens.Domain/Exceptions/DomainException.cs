namespace PergamonLens.Domain.Exceptions;

/// <summary>
/// Excepción base para todas las infracciones de reglas de negocio del dominio.
/// </summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>
/// Excepción lanzada cuando un código de barras no cumple el formato o el checksum GS1.
/// </summary>
public sealed class InvalidBarcodeException(string message) : DomainException(message);

/// <summary>
/// Excepción lanzada cuando un número de catálogo es inválido o está vacío.
/// </summary>
public sealed class InvalidCatalogueNumberException(string message) : DomainException(message);

/// <summary>
/// Excepción lanzada cuando el año de lanzamiento está fuera del rango histórico permitido.
/// </summary>
public sealed class InvalidReleaseYearException(string message) : DomainException(message);

/// <summary>
/// Excepción lanzada cuando una operación o regla sobre un lanzamiento musical es inválida.
/// </summary>
public class MusicReleaseDomainException(string message) : DomainException(message);

/// <summary>
/// Excepción lanzada cuando una operación o regla sobre un vinilo es inválida.
/// </summary>
public sealed class VinylRecordDomainException(string message) : MusicReleaseDomainException(message);

/// <summary>
/// Excepción lanzada cuando los datos de una pista de audio son inválidos.
/// </summary>
public sealed class InvalidTrackException(string message) : DomainException(message);

