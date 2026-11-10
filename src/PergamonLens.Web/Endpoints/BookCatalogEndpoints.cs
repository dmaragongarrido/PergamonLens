using MediatR;
using Microsoft.AspNetCore.Mvc;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.MediaReleases.Queries.SearchBookCatalog;

namespace PergamonLens.Web.Endpoints;

/// <summary>
/// Endpoints RESTful de Minimal APIs para la búsqueda canónica de libros en OpenLibrary.
/// </summary>
public static class BookCatalogEndpoints
{
    public static IEndpointRouteBuilder MapBookCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/books")
            .RequireAuthorization()
            .WithTags("Book Catalog (OpenLibrary)");

        group.MapGet("/search", async (
            [FromQuery] string? isbn,
            [FromQuery] string? author,
            [FromQuery] string? title,
            [FromQuery] string? query,
            [FromQuery] string? q,
            ISender sender,
            CancellationToken ct) =>
        {
            var freeText = !string.IsNullOrWhiteSpace(query) ? query : q;

            var searchQuery = new SearchBookCatalogQuery(
                Isbn: isbn,
                Author: author,
                Title: title,
                FreeText: freeText);

            var result = await sender.Send(searchQuery, ct);
            return Results.Ok(result);
        })
        .WithName("SearchBookCatalog")
        .WithSummary("Busca ediciones bibliográficas en OpenLibrary por ISBN, Autor/Título o texto libre")
        .Produces<IReadOnlyList<BookCatalogSearchResultDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}

