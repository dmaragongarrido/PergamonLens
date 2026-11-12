using MediatR;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MediaReleases.Commands.ConfirmRelease;

/// <summary>
/// Manejador del comando para confirmar el lanzamiento en la colección personal del coleccionista.
/// </summary>
public sealed class ConfirmReleaseCommandHandler : IRequestHandler<ConfirmReleaseCommand, MediaReleaseDto>
{
    private readonly IMediaReleaseRepository _repository;

    public ConfirmReleaseCommandHandler(IMediaReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MediaReleaseDto> Handle(ConfirmReleaseCommand request, CancellationToken cancellationToken)
    {
        var releaseId = new MediaReleaseId(request.ReleaseId);
        var release = await _repository.GetByIdAsync(releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(MediaRelease), request.ReleaseId);
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

