using FluentValidation;

namespace PergamonLens.Application.MusicReleases.Commands.ConfirmRelease;

/// <summary>
/// Validador de reglas para la confirmación de un lanzamiento en la colección personal.
/// </summary>
public sealed class ConfirmReleaseCommandValidator : AbstractValidator<ConfirmReleaseCommand>
{
    public ConfirmReleaseCommandValidator()
    {
        RuleFor(x => x.ReleaseId)
            .NotEmpty().WithMessage("El identificador del lanzamiento musical es obligatorio.");

        RuleFor(x => x.MediaCondition)
            .IsInEnum().WithMessage("La graduación del estado de conservación del soporte (MediaCondition) no es válida.");

        RuleFor(x => x.SleeveCondition)
            .IsInEnum().WithMessage("La graduación del estado de conservación de la funda/caja (SleeveCondition) no es válida.");

        When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("Las notas personales no pueden superar los 2000 caracteres.");
        });
    }
}

