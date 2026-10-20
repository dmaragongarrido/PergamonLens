using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Queries.GetCollectionStatistics;

/// <summary>
/// Consulta para obtener las estadísticas consolidadas de la colección física.
/// </summary>
public sealed record GetCollectionStatisticsQuery : IRequest<CollectionStatisticsDto>;

