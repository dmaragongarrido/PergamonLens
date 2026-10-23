using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PergamonLens.Application.Common.Interfaces;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.Identity;

namespace PergamonLens.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de Entity Framework Core para la persistencia en SQL Server,
/// extendido con soporte para ASP.NET Core Identity y aislamiento multi-usuario.
/// </summary>
public sealed class PergamonLensDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUserService;

    public DbSet<MusicRelease> MusicReleases => Set<MusicRelease>();
    public DbSet<Track> Tracks => Set<Track>();

    public PergamonLensDbContext(
        DbContextOptions<PergamonLensDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    private UserId? CurrentUser => UserId.TryFrom(_currentUserService?.UserId);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PergamonLensDbContext).Assembly);

        // Global Query Filter para aislamiento multi-usuario irrefutable (OWASP BOLA / IDOR)
        // Si no hay usuario autenticado (ej. tareas en segundo plano o migraciones), no se filtra.
        modelBuilder.Entity<MusicRelease>()
            .HasQueryFilter(r => CurrentUser == null || r.UserId == CurrentUser);
    }
}
