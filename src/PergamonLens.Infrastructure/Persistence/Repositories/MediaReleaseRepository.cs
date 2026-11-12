using Microsoft.EntityFrameworkCore;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación con Entity Framework Core del repositorio de lanzamientos y ejemplares físicos (<see cref="IMediaReleaseRepository"/>).
/// </summary>
public sealed class MediaReleaseRepository : IMediaReleaseRepository
{
    private readonly PergamonLensDbContext _context;

    public MediaReleaseRepository(PergamonLensDbContext context)
    {
        _context = context;
    }

    public async Task<MediaRelease?> GetByIdAsync(MediaReleaseId id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaReleases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaRelease>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MediaReleases
            .Include(r => r.Tracks)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaRelease>> GetByFormatAsync(MediaFormat format, CancellationToken cancellationToken = default)
    {
        return await _context.MediaReleases
            .Include(r => r.Tracks)
            .Where(r => r.Format == format)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaRelease>> GetByStatusAsync(ProcessingStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.MediaReleases
            .Include(r => r.Tracks)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaRelease>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllAsync(cancellationToken);
        }

        string normalized = query.Trim();

        var userReleases = await _context.MediaReleases
            .Include(r => r.Tracks)
            .ToListAsync(cancellationToken);

        return userReleases
            .Where(r =>
                r.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                r.Artist.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (r.Label != null && r.Label.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (r.Country != null && r.Country.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (r.Platform != null && r.Platform.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (r.Genre != null && r.Genre.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (r.Barcode != null && r.Barcode.Value.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (r.CatalogueNumber != null && r.CatalogueNumber.Value.Contains(normalized, StringComparison.OrdinalIgnoreCase)) ||
                (r.ReleaseYear != null && r.ReleaseYear.Value.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(r => r.Artist)
            .ThenBy(r => r.Title)
            .ToList();
    }

    public async Task AddAsync(MediaRelease release, CancellationToken cancellationToken = default)
    {
        await _context.MediaReleases.AddAsync(release, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MediaRelease release, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(release).State == EntityState.Detached)
        {
            _context.MediaReleases.Update(release);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MediaRelease release, CancellationToken cancellationToken = default)
    {
        _context.MediaReleases.Remove(release);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(MediaReleaseId id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaReleases.AnyAsync(r => r.Id == id, cancellationToken);
    }
}
