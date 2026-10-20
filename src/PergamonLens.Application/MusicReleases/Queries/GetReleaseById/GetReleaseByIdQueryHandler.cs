using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Application.MusicReleases.Queries.GetReleaseById;

/// <summary>
/// Manejador para consultar un lanzamiento musical por su identificador.
/// </summary>
public sealed class GetReleaseByIdQueryHandler : IRequestHandler<GetReleaseByIdQuery, MusicReleaseDto?>
{
    private readonly IMusicReleaseRepository _repository;

    public GetReleaseByIdQueryHandler(IMusicReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<MusicReleaseDto?> Handle(GetReleaseByIdQuery request, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByIdAsync(new MusicReleaseId(request.Id), cancellationToken);

        return release?.ToDto();
    }
}

