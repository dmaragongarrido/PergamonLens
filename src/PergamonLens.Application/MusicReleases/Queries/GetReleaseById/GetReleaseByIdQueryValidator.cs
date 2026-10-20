using FluentValidation;

namespace PergamonLens.Application.MusicReleases.Queries.GetReleaseById;

/// <summary>
/// Validador de la consulta por identificador.
/// </summary>
public sealed class GetReleaseByIdQueryValidator : AbstractValidator<GetReleaseByIdQuery>
{
    public GetReleaseByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador del lanzamiento musical es obligatorio.");
    }
}

