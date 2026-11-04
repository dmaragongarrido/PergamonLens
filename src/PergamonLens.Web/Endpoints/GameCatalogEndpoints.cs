using MediatR;
using Microsoft.AspNetCore.Mvc;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.MusicReleases.Queries.SearchGameCatalog;

namespace PergamonLens.Web.Endpoints;

/// <summary>
/// Endpoints RESTful de Minimal APIs para la búsqueda canónica de videojuegos en el catálogo oficial.
/// </summary>
public static class GameCatalogEndpoints
{
    public static IEndpointRouteBuilder MapGameCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/games")
            .RequireAuthorization()
            .WithTags("Video Game Catalog");

        group.MapGet("/search", async (
            [FromQuery] string? title,
            [FromQuery] string? platform,
            [FromQuery] string? query,
            [FromQuery] string? q,
            ISender sender,
            CancellationToken ct) =>
        {
            var freeText = !string.IsNullOrWhiteSpace(query) ? query : q;

            var searchQuery = new SearchGameCatalogQuery(
                Title: title,
                PlatformOrStudio: platform,
                FreeText: freeText);

            var result = await sender.Send(searchQuery, ct);
            return Results.Ok(result);
        })
        .WithName("SearchGameCatalog")
        .WithSummary("Busca videojuegos físicos de consola, retro y PC en el catálogo oficial")
        .Produces<IReadOnlyList<GameCatalogSearchResultDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}

