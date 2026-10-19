using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para el agregado <see cref="MusicRelease"/>.
/// </summary>
public interface IMusicReleaseRepository
{
    Task<MusicRelease?> GetByIdAsync(MusicReleaseId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> GetByFormatAsync(MediaFormat format, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> GetByStatusAsync(ProcessingStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task AddAsync(MusicRelease release, CancellationToken cancellationToken = default);

    Task UpdateAsync(MusicRelease release, CancellationToken cancellationToken = default);

    Task DeleteAsync(MusicRelease release, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(MusicReleaseId id, CancellationToken cancellationToken = default);
}

