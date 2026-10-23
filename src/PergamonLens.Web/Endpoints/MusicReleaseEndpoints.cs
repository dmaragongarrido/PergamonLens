using MediatR;
using Microsoft.AspNetCore.Mvc;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.MusicReleases.Commands.ConfirmRelease;
using PergamonLens.Application.MusicReleases.Commands.CreateRelease;
using PergamonLens.Application.MusicReleases.Commands.DeleteRelease;
using PergamonLens.Application.MusicReleases.Commands.EnrichReleaseFromDiscogs;
using PergamonLens.Application.MusicReleases.Commands.ScanRelease;
using PergamonLens.Application.MusicReleases.Queries.GetCollection;
using PergamonLens.Application.MusicReleases.Queries.GetReleaseById;
using PergamonLens.Domain.Enums;

namespace PergamonLens.Web.Endpoints;

/// <summary>
/// Endpoints RESTful de Minimal APIs para la gestión del catálogo de lanzamientos musicales.
/// </summary>
public static class MusicReleaseEndpoints
{
    public static IEndpointRouteBuilder MapMusicReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/releases")
            .RequireAuthorization()
            .WithTags("Music Releases");

        group.MapPost("/", async (
            [FromBody] CreateReleaseCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/releases/{result.Id}", result);
        })
        .WithName("CreateRelease")
        .WithSummary("Crea manualmente un lanzamiento musical en la colección personal")
        .Produces<MusicReleaseDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/scan", async (
            IFormFile file,
            [FromForm] MediaFormat? format,
            ISender sender,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Archivo requerido",
                    Detail = "Debe proporcionar una imagen válida (portada, contraportada o código de barras) para el análisis."
                });
            }

            await using var stream = file.OpenReadStream();
            var command = new ScanReleaseCommand(
                ImageStream: stream,
                ContentType: file.ContentType,
                Format: format ?? MediaFormat.Vinyl);

            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .DisableAntiforgery()
        .WithName("ScanRelease")
        .WithSummary("Ingiere y analiza una fotografía de álbum físico mediante IA multimodal")
        .Produces<MusicReleaseDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async (
            [FromQuery] string? q,
            [FromQuery] MediaFormat? format,
            [FromQuery] ProcessingStatus? status,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetCollectionQuery(
                Format: format,
                Status: status,
                SearchQuery: q);

            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetReleases")
        .WithSummary("Obtiene los lanzamientos de la colección del usuario con filtros opcionales")
        .Produces<IReadOnlyList<MusicReleaseDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetReleaseByIdQuery(id), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Lanzamiento no encontrado",
                    Detail = $"No se encontró ningún lanzamiento musical con identificador '{id}' en su colección."
                });
        })
        .WithName("GetReleaseById")
        .WithSummary("Obtiene los detalles de un lanzamiento musical por su identificador")
        .Produces<MusicReleaseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            [FromBody] ConfirmReleaseRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new ConfirmReleaseCommand(
                ReleaseId: id,
                MediaCondition: request.MediaCondition,
                SleeveCondition: request.SleeveCondition,
                Notes: request.Notes);

            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ConfirmRelease")
        .WithSummary("Confirma y califica las condiciones físicas de conservación de un lanzamiento (Goldmine Grading)")
        .Produces<MusicReleaseDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/{id:guid}/enrich", async (
            Guid id,
            [FromBody] EnrichReleaseRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new EnrichReleaseFromDiscogsCommand(
                ReleaseId: id,
                DiscogsReleaseId: request.DiscogsReleaseId);

            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("EnrichReleaseFromDiscogs")
        .WithSummary("Enriquece los metadatos y lista de pistas de un lanzamiento desde la API canónica de Discogs")
        .Produces<MusicReleaseDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteReleaseCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteRelease")
        .WithSummary("Elimina un lanzamiento musical y sus pistas asociadas de la colección")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}

public sealed record ConfirmReleaseRequest(
    MediaCondition MediaCondition,
    MediaCondition SleeveCondition,
    string? Notes = null);

public sealed record EnrichReleaseRequest(
    long DiscogsReleaseId);

