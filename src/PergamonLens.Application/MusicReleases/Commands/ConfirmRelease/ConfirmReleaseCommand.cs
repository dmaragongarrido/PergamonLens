using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Enums;

namespace PergamonLens.Application.MusicReleases.Commands.ConfirmRelease;

/// <summary>
/// Comando para confirmar un lanzamiento musical en la colección personal,
/// asignando la graduación del estado de conservación física (Goldmine Standard).
/// </summary>
public sealed record ConfirmReleaseCommand(
    Guid ReleaseId,
    MediaCondition MediaCondition,
    MediaCondition SleeveCondition,
    string? Notes = null) : IRequest<MusicReleaseDto>;

