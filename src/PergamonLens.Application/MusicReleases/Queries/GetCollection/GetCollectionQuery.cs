using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Enums;

namespace PergamonLens.Application.MusicReleases.Queries.GetCollection;

/// <summary>
/// Consulta para listar lanzamientos de la colección con filtros opcionales de formato, estado o búsqueda textual.
/// </summary>
public sealed record GetCollectionQuery(
    MediaFormat? Format = null,
    ProcessingStatus? Status = null,
    string? SearchQuery = null) : IRequest<IReadOnlyList<MusicReleaseDto>>;

