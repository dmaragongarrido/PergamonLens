using FluentValidation;

namespace PergamonLens.Application.MusicReleases.Commands.DeleteRelease;

/// <summary>
/// Validador para la eliminación de un lanzamiento musical.
/// </summary>
public sealed class DeleteReleaseCommandValidator : AbstractValidator<DeleteReleaseCommand>
{
    public DeleteReleaseCommandValidator()
    {
        RuleFor(x => x.ReleaseId)
            .NotEmpty().WithMessage("El identificador del lanzamiento a eliminar es obligatorio.");
    }
}

