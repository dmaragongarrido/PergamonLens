using Microsoft.EntityFrameworkCore;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación con Entity Framework Core del repositorio de lanzamientos musicales (<see cref="IMusicReleaseRepository"/>).
/// </summary>
public sealed class MusicReleaseRepository : IMusicReleaseRepository
{
    private readonly PergamonLensDbContext _context;

    public MusicReleaseRepository(PergamonLensDbContext context)
    {
        _context = context;
    }

    public async Task<MusicRelease?> GetByIdAsync(MusicReleaseId id, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MusicRelease>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MusicRelease>> GetByFormatAsync(MediaFormat format, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .Where(r => r.Format == format)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MusicRelease>> GetByStatusAsync(ProcessingStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MusicRelease>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllAsync(cancellationToken);
        }

        string normalized = query.Trim();

        return await _context.MusicReleases
            .Include(r => r.Tracks)
            .Where(r =>
                r.Title.Contains(normalized) ||
                r.Artist.Contains(normalized) ||
                (r.Label != null && r.Label.Contains(normalized)) ||
                (r.Barcode != null && ((string)r.Barcode).Contains(normalized)) ||
                (r.CatalogueNumber != null && ((string)r.CatalogueNumber).Contains(normalized)))
            .OrderBy(r => r.Artist)
            .ThenBy(r => r.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MusicRelease release, CancellationToken cancellationToken = default)
    {
        await _context.MusicReleases.AddAsync(release, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MusicRelease release, CancellationToken cancellationToken = default)
    {
        _context.MusicReleases.Update(release);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MusicRelease release, CancellationToken cancellationToken = default)
    {
        _context.MusicReleases.Remove(release);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(MusicReleaseId id, CancellationToken cancellationToken = default)
    {
        return await _context.MusicReleases.AnyAsync(r => r.Id == id, cancellationToken);
    }
}
