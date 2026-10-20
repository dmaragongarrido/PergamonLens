using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Queries.GetReleaseById;

/// <summary>
/// Consulta para obtener los detalles completos de un lanzamiento musical por su ID.
/// </summary>
public sealed record GetReleaseByIdQuery(Guid Id) : IRequest<MusicReleaseDto?>;

