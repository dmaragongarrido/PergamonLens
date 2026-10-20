using FluentValidation;

namespace PergamonLens.Application.MusicReleases.Commands.EnrichReleaseFromDiscogs;

/// <summary>
/// Validador de reglas para el enriquecimiento desde Discogs.
/// </summary>
public sealed class EnrichReleaseFromDiscogsCommandValidator : AbstractValidator<EnrichReleaseFromDiscogsCommand>
{
    public EnrichReleaseFromDiscogsCommandValidator()
    {
        RuleFor(x => x.ReleaseId)
            .NotEmpty().WithMessage("El identificador del lanzamiento musical es obligatorio.");

        RuleFor(x => x.DiscogsReleaseId)
            .GreaterThan(0).WithMessage("El identificador de edición de Discogs debe ser un número positivo.");
    }
}

