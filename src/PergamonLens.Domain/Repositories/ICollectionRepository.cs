using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Repositories;

/// <summary>
/// Contrato para consultas transversales y agregadas sobre la colección personal de lanzamientos musicales.
/// </summary>
public interface ICollectionRepository
{
    Task<CollectionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> GetRecentAdditionsAsync(int count = 10, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> GetByArtistAsync(string artist, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MusicRelease>> GetByFormatAsync(MediaFormat format, CancellationToken cancellationToken = default);
}
