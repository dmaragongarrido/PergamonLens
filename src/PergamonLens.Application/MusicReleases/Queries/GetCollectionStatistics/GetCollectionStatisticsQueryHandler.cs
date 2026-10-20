using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Application.Mappings;
using PergamonLens.Domain.Repositories;

namespace PergamonLens.Application.MusicReleases.Queries.GetCollectionStatistics;

/// <summary>
/// Manejador de la consulta de estadísticas agregadas de la colección.
/// </summary>
public sealed class GetCollectionStatisticsQueryHandler : IRequestHandler<GetCollectionStatisticsQuery, CollectionStatisticsDto>
{
    private readonly ICollectionRepository _repository;

    public GetCollectionStatisticsQueryHandler(ICollectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CollectionStatisticsDto> Handle(GetCollectionStatisticsQuery request, CancellationToken cancellationToken)
    {
        var statistics = await _repository.GetStatisticsAsync(cancellationToken);

        return statistics.ToDto();
    }
}

