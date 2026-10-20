using MediatR;

namespace PergamonLens.Application.MusicReleases.Commands.DeleteRelease;

/// <summary>
/// Comando para eliminar un lanzamiento musical de la colección.
/// </summary>
public sealed record DeleteReleaseCommand(Guid ReleaseId) : IRequest;

