using MediatR;
using Microsoft.AspNetCore.Mvc;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.MusicReleases.Queries.SearchDiscogs;

namespace PergamonLens.Web.Endpoints;

/// <summary>
/// Endpoints RESTful de Minimal APIs para la búsqueda canónica en el catálogo de Discogs.
/// </summary>
public static class DiscogsEndpoints
{
    public static IEndpointRouteBuilder MapDiscogsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/discogs")
            .RequireAuthorization()
            .WithTags("Discogs Catalog");

        group.MapGet("/search", async (
            [FromQuery] string? barcode,
            [FromQuery] string? catalogueNumber,
            [FromQuery] string? artist,
            [FromQuery] string? title,
            [FromQuery] string? query,
            [FromQuery] string? q,
            ISender sender,
            CancellationToken ct) =>
        {
            var freeText = !string.IsNullOrWhiteSpace(query) ? query : q;

            var searchQuery = new SearchDiscogsQuery(
                Barcode: barcode,
                CatalogueNumber: catalogueNumber,
                Artist: artist,
                Title: title,
                Query: freeText);

            var result = await sender.Send(searchQuery, ct);
            return Results.Ok(result);
        })
        .WithName("SearchDiscogs")
        .WithSummary("Busca referencias, ediciones y sellos en la base de datos canónica de Discogs")
        .Produces<IReadOnlyList<DiscogsSearchResultDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
