using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Domain.Exceptions;

namespace PergamonLens.Web.Infrastructure;

/// <summary>
/// Manejador global de excepciones para traducir errores de dominio y aplicación
/// en respuestas HTTP estandarizadas conforme al estándar RFC 7807 (ProblemDetails).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Se ha producido una excepción durante el procesamiento de la petición: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación",
                Detail = "Uno o más campos de la solicitud no superaron las reglas de validación.",
                Extensions = { ["errors"] = validationEx.Errors }
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso no encontrado",
                Detail = notFoundEx.Message
            },
            DomainException domainEx => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Infracción de regla de negocio",
                Detail = domainEx.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor",
                Detail = "Ha ocurrido un error inesperado al procesar la solicitud."
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

