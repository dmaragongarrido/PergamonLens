using FluentValidation;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Commands.CreateRelease;

/// <summary>
/// Validador de reglas para la creación manual de un lanzamiento musical.
/// </summary>
public sealed class CreateReleaseCommandValidator : AbstractValidator<CreateReleaseCommand>
{
    public CreateReleaseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título del lanzamiento es obligatorio.")
            .MaximumLength(500).WithMessage("El título no puede superar los 500 caracteres.");

        RuleFor(x => x.Artist)
            .NotEmpty().WithMessage("El artista del lanzamiento es obligatorio.")
            .MaximumLength(500).WithMessage("El artista no puede superar los 500 caracteres.");

        RuleFor(x => x.DiscsCount)
            .GreaterThanOrEqualTo(1).WithMessage("El número de discos o soportes debe ser como mínimo 1.");

        RuleFor(x => x.Format)
            .IsInEnum().WithMessage("El formato del medio físico especificado no es válido.");

        When(x => !string.IsNullOrWhiteSpace(x.Barcode), () =>
        {
            RuleFor(x => x.Barcode)
                .Must(b => Barcode.TryCreate(b, out _, out _))
                .WithMessage("El código de barras proporcionado no cumple con el estándar GS1/EAN/UPC.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.CatalogueNumber), () =>
        {
            RuleFor(x => x.CatalogueNumber)
                .Must(c => CatalogueNumber.TryCreate(c, out _, out _))
                .WithMessage($"El número de catálogo no es válido o supera los {CatalogueNumber.MaxLength} caracteres.");
        });

        RuleFor(x => x.ReleaseYear)
            .Must(y => !y.HasValue || ReleaseYear.TryCreate(y.Value, out _, out _))
            .WithMessage("El año de lanzamiento está fuera del rango histórico permitido (1877 a la actualidad).");

        When(x => x.Format == MediaFormat.Vinyl, () =>
        {
            RuleFor(x => x.VinylSize)
                .IsInEnum()
                .When(x => x.VinylSize.HasValue)
                .WithMessage("El tamaño del vinilo especificado no es válido.");

            RuleFor(x => x.VinylSpeed)
                .IsInEnum()
                .When(x => x.VinylSpeed.HasValue)
                .WithMessage("La velocidad del vinilo especificada no es válida.");
        });
    }
}

