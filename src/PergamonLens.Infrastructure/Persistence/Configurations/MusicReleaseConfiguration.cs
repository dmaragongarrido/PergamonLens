using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo Fluent API para el Aggregate Root <see cref="MusicRelease"/>.
/// </summary>
public sealed class MusicReleaseConfiguration : IEntityTypeConfiguration<MusicRelease>
{
    public void Configure(EntityTypeBuilder<MusicRelease> builder)
    {
        builder.ToTable("MusicReleases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, val => new MusicReleaseId(val));

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasConversion(id => id.Value, val => UserId.From(val))
            .HasMaxLength(450);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Artist)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Format)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Label)
            .HasMaxLength(300);

        builder.Property(x => x.Country)
            .HasMaxLength(100);

        builder.Property(x => x.CoverImageUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.DiscsCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.VinylSize)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.VinylSpeed)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.MediaCondition)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SleeveCondition)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.DiscogsReleaseId);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        // Value Objects
        builder.Property(x => x.Barcode)
            .HasConversion(
                b => b != null ? b.Value : null,
                v => !string.IsNullOrEmpty(v) ? Barcode.Create(v) : null)
            .HasMaxLength(20);

        builder.Property(x => x.CatalogueNumber)
            .HasConversion(
                c => c != null ? c.Value : null,
                v => !string.IsNullOrEmpty(v) ? CatalogueNumber.Create(v) : null)
            .HasMaxLength(60);

        builder.Property(x => x.ReleaseYear)
            .HasConversion(
                y => y != null ? y.Value : (int?)null,
                v => v.HasValue ? ReleaseYear.Create(v.Value) : null);

        // Relación con Tracks (Aggregate Root encapsula sus entidades internas)
        builder.HasMany(x => x.Tracks)
            .WithOne()
            .HasForeignKey("MusicReleaseId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Tracks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Índices para optimizar búsquedas, aislamiento multi-usuario y consultas frecuentes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Format });
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.CatalogueNumber);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Format);
        builder.HasIndex(x => new { x.Artist, x.Title });
    }
}
