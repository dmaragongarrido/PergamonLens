using FluentValidation;

namespace PergamonLens.Application.MusicReleases.Commands.ScanRelease;

/// <summary>
/// Validador de reglas para la ingesta y escaneo visual de lanzamientos.
/// </summary>
public sealed class ScanReleaseCommandValidator : AbstractValidator<ScanReleaseCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    public ScanReleaseCommandValidator()
    {
        RuleFor(x => x.ImageStream)
            .NotNull().WithMessage("El flujo de datos de la imagen es obligatorio.")
            .Must(s => s is { CanRead: true }).WithMessage("El flujo de la imagen debe ser legible.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("El tipo de contenido (Content-Type) de la imagen es obligatorio.")
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("El formato de imagen no es compatible. Se admiten JPEG, PNG y WebP.");

        RuleFor(x => x.Format)
            .IsInEnum().WithMessage("El formato del medio físico especificado no es válido.");
    }
}

