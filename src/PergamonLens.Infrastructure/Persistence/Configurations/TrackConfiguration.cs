using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PergamonLens.Domain.Entities;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo Fluent API para la entidad interna <see cref="Track"/>.
/// </summary>
public sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("Tracks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Position)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Duration);

        // Clave foránea fuertemente tipada hacia la raíz de agregado MusicRelease
        builder.Property<MusicReleaseId>("MusicReleaseId")
            .HasConversion(id => id.Value, val => new MusicReleaseId(val))
            .IsRequired();

        builder.HasIndex("MusicReleaseId", "Position");
    }
}
