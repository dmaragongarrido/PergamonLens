using MediatR;
using PergamonLens.Application.DTOs;
using PergamonLens.Domain.Enums;

namespace PergamonLens.Application.MusicReleases.Commands.ScanRelease;

/// <summary>
/// Comando para ingerir una imagen (fotografía de portada o código de barras)
/// y orquestar su análisis mediante inteligencia artificial visual multimodal.
/// </summary>
public sealed record ScanReleaseCommand(
    Stream ImageStream,
    string ContentType,
    MediaFormat Format = MediaFormat.Vinyl) : IRequest<MusicReleaseDto>;

