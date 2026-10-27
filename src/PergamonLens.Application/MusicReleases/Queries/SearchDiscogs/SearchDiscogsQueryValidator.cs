using FluentValidation;

namespace PergamonLens.Application.MusicReleases.Queries.SearchDiscogs;

/// <summary>
/// Validador de los parámetros de búsqueda en Discogs.
/// Requiere que al menos se proporcione un criterio de búsqueda (código de barras, catálogo, texto libre o artista+título).
/// </summary>
public sealed class SearchDiscogsQueryValidator : AbstractValidator<SearchDiscogsQuery>
{
    public SearchDiscogsQueryValidator()
    {
        RuleFor(x => x)
            .Must(HaveAtLeastOneSearchCriterion)
            .WithMessage("Debe proporcionar al menos un criterio de búsqueda: código de barras, número de catálogo, texto libre o artista y título.");
    }

    private static bool HaveAtLeastOneSearchCriterion(SearchDiscogsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Barcode))
            return true;

        if (!string.IsNullOrWhiteSpace(query.CatalogueNumber))
            return true;

        if (!string.IsNullOrWhiteSpace(query.Query))
            return true;

        return !string.IsNullOrWhiteSpace(query.Artist) && !string.IsNullOrWhiteSpace(query.Title);
    }
}

