using MediatR;
using PergamonLens.Application.Common.Exceptions;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MediaReleases.Commands.DeleteRelease;

/// <summary>
/// Manejador de la eliminación de un lanzamiento de la colección.
/// </summary>
public sealed class DeleteReleaseCommandHandler : IRequestHandler<DeleteReleaseCommand>
{
    private readonly IMediaReleaseRepository _repository;

    public DeleteReleaseCommandHandler(IMediaReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteReleaseCommand request, CancellationToken cancellationToken)
    {
        var releaseId = new MediaReleaseId(request.ReleaseId);
        var release = await _repository.GetByIdAsync(releaseId, cancellationToken);

        if (release is null)
        {
            throw new NotFoundException(nameof(MediaRelease), request.ReleaseId);
        }

        await _repository.DeleteAsync(release, cancellationToken);
    }
}

