using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Repositories;

/// <summary>
/// Contrato de persistencia para el agregado <see cref="VinylRecord"/>.
/// </summary>
public interface IVinylRecordRepository
{
    Task<VinylRecord?> GetByIdAsync(VinylRecordId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VinylRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VinylRecord>> GetByStatusAsync(ProcessingStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VinylRecord>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task AddAsync(VinylRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(VinylRecord record, CancellationToken cancellationToken = default);

    Task DeleteAsync(VinylRecord record, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(VinylRecordId id, CancellationToken cancellationToken = default);
}
