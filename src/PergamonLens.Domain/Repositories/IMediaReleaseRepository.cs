using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para el agregado <see cref="MediaRelease"/>.
/// </summary>
public interface IMediaReleaseRepository
{
    Task<MediaRelease?> GetByIdAsync(MediaReleaseId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaRelease>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaRelease>> GetByFormatAsync(MediaFormat format, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaRelease>> GetByStatusAsync(ProcessingStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaRelease>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task AddAsync(MediaRelease release, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaRelease release, CancellationToken cancellationToken = default);

    Task DeleteAsync(MediaRelease release, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(MediaReleaseId id, CancellationToken cancellationToken = default);
}
