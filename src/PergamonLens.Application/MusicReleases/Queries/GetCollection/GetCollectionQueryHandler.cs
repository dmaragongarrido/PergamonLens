using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Repositories;

namespace PergamonLens.Application.MusicReleases.Queries.GetCollection;

/// <summary>
/// Manejador de la consulta de colección de lanzamientos físicos.
/// </summary>
public sealed class GetCollectionQueryHandler : IRequestHandler<GetCollectionQuery, IReadOnlyList<MusicReleaseDto>>
{
    private readonly IMusicReleaseRepository _repository;

    public GetCollectionQueryHandler(IMusicReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<MusicReleaseDto>> Handle(GetCollectionQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<MusicRelease> releases;

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            releases = await _repository.SearchAsync(request.SearchQuery, cancellationToken);
        }
        else if (request.Format.HasValue)
        {
            releases = await _repository.GetByFormatAsync(request.Format.Value, cancellationToken);
        }
        else if (request.Status.HasValue)
        {
            releases = await _repository.GetByStatusAsync(request.Status.Value, cancellationToken);
        }
        else
        {
            releases = await _repository.GetAllAsync(cancellationToken);
        }

        // Aplicar filtros adicionales en memoria si se especificaron simultáneamente
        var filtered = releases.AsEnumerable();

        if (request.Format.HasValue && !string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            filtered = filtered.Where(r => r.Format == request.Format.Value);
        }

        if (request.Status.HasValue && (!string.IsNullOrWhiteSpace(request.SearchQuery) || request.Format.HasValue))
        {
            filtered = filtered.Where(r => r.Status == request.Status.Value);
        }

        return filtered
            .Select(r => r.ToDto())
            .ToList();
    }
}

