using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MediaReleases.Queries.GetReleaseById;

/// <summary>
/// Manejador para consultar un lanzamiento musical por su identificador.
/// </summary>
public sealed class GetReleaseByIdQueryHandler : IRequestHandler<GetReleaseByIdQuery, MediaReleaseDto?>
{
    private readonly IMediaReleaseRepository _repository;

    public GetReleaseByIdQueryHandler(IMediaReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MediaReleaseDto?> Handle(GetReleaseByIdQuery request, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(new MediaReleaseId(request.Id), cancellationToken);

        return release?.ToDto();
    }
}

