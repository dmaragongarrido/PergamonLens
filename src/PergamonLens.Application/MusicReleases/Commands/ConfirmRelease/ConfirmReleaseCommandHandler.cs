using MediatR;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Commands.ConfirmRelease;

/// <summary>
/// Manejador del comando para confirmar el lanzamiento en la colección personal del coleccionista.
/// </summary>
public sealed class ConfirmReleaseCommandHandler : IRequestHandler<ConfirmReleaseCommand, MusicReleaseDto>
{
    private readonly IMusicReleaseRepository _repository;

    public ConfirmReleaseCommandHandler(IMusicReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MusicReleaseDto> Handle(ConfirmReleaseCommand request, CancellationToken cancellationToken)
    {
        var releaseId = new MusicReleaseId(request.ReleaseId);
        var release = await _repository.GetByIdAsync(releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(MusicRelease), request.ReleaseId);
        }

        release.SetGrading(request.MediaCondition, request.SleeveCondition);

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            release.UpdateNotes(request.Notes);
        }

        release.Confirm();

        await _repository.UpdateAsync(release, cancellationToken);

        return release.ToDto();
    }
}

