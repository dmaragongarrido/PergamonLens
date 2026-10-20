using FluentValidation.Results;

namespace PergamonLens.Application.Common.Exceptions;

/// <summary>
/// Excepción lanzada cuando una o más reglas de validación de FluentValidation son infringidas.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Se han producido uno o más errores de validación.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(string propertyName, string errorMessage)
        : this()
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

