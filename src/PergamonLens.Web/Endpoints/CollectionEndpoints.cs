using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.MusicReleases.Queries.GetCollectionStatistics;

namespace PergamonLens.Web.Endpoints;

/// <summary>
/// Endpoints RESTful de Minimal APIs para la gestión agregada y estadísticas de la colección.
/// </summary>
public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collection")
            .RequireAuthorization()
            .WithTags("Collection Analytics");

        group.MapGet("/statistics", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCollectionStatisticsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetCollectionStatistics")
        .WithSummary("Obtiene las estadísticas y métricas acumuladas de la colección del usuario autenticado")
        .Produces<CollectionStatisticsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
