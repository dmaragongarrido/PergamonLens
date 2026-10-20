using MediatR;
using PergamonLens.Application.DTOs;

namespace PergamonLens.Application.MusicReleases.Commands.EnrichReleaseFromDiscogs;

/// <summary>
/// Comando para contrastar y enriquecer un lanzamiento musical con los datos canónicos de Discogs.
/// </summary>
public sealed record EnrichReleaseFromDiscogsCommand(
    Guid ReleaseId,
    long DiscogsReleaseId) : IRequest<MusicReleaseDto>;

