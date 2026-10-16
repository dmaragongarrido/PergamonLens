using PergamonLens.Domain.Entities;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Repositories;

/// <summary>
/// Contrato para consultas transversales y agregadas sobre la colección personal de vinilos.
/// </summary>
public interface ICollectionRepository
{
    Task<CollectionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VinylRecord>> GetRecentAdditionsAsync(int count = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VinylRecord>> GetByArtistAsync(string artist, CancellationToken cancellationToken = default);
}
