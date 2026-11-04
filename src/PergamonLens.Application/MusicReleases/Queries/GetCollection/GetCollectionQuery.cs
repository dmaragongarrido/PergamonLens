using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Enums;

namespace PergamonLens.Application.MusicReleases.Queries.GetCollection;

/// <summary>
/// Consulta para listar ejemplares de la colección con filtros opcionales de categoría, formato, estado o búsqueda textual.
/// </summary>
public sealed record GetCollectionQuery(
    MediaFormat? Format = null,
    ProcessingStatus? Status = null,
    string? SearchQuery = null,
    MediaCategory? Category = null) : IRequest<IReadOnlyList<MusicReleaseDto>>;
