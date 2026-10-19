using PergamonLens.Domain.Common;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Events;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Entities;

/// <summary>
/// Raíz de agregado (Aggregate Root) que representa un lanzamiento musical físico en la colección
/// (Vinilo, CD, Casete o Box Set).
/// Encapsula sus metadatos discográficos, formato físico, estado de conservación y lista de pistas.
/// </summary>
public sealed class MusicRelease : AggregateRoot<MusicReleaseId>
{
    private readonly List<Track> _tracks = [];

    public MediaFormat Format { get; private set; } = MediaFormat.Vinyl;
    public string Title { get; private set; } = string.Empty;
    public string Artist { get; private set; } = string.Empty;
    public string? Label { get; private set; }
    public ReleaseYear? ReleaseYear { get; private set; }
    public Barcode? Barcode { get; private set; }
    public CatalogueNumber? CatalogueNumber { get; private set; }
    public string? Country { get; private set; }
    public string? CoverImageUrl { get; private set; }

    public int DiscsCount { get; private set; } = 1;

    // Atributos específicos cuando el formato es vinilo
    public VinylSize? VinylSize { get; private set; }
    public VinylSpeed? VinylSpeed { get; private set; }

    // Graduación del estado de conservación (Goldmine standard)
    public MediaCondition? MediaCondition { get; private set; }
    public MediaCondition? SleeveCondition { get; private set; }

    // Ciclo de vida y procesamiento inteligente
    public ProcessingStatus Status { get; private set; } = ProcessingStatus.PendingAnalysis;
    public long? DiscogsReleaseId { get; private set; }
    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Track> Tracks => _tracks.AsReadOnly();

    private MusicRelease(
        MusicReleaseId id,
        MediaFormat format,
        string title,
        string artist,
        Barcode? barcode,
        CatalogueNumber? catalogueNumber,
        ReleaseYear? releaseYear,
        int discsCount,
        VinylSize? vinylSize,
        VinylSpeed? vinylSpeed,
        string? label,
        string? country,
        string? coverImageUrl,
        string? notes,
        ProcessingStatus status,
        DateTime createdAtUtc) : base(id)
    {
        Format = format;
        Title = title;
        Artist = artist;
        Barcode = barcode;
        CatalogueNumber = catalogueNumber;
        ReleaseYear = releaseYear;
        DiscsCount = discsCount;
        VinylSize = vinylSize;
        VinylSpeed = vinylSpeed;
        Label = label;
        Country = country;
        CoverImageUrl = coverImageUrl;
        Notes = notes;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    // Constructor privado requerido por EF Core
    private MusicRelease() { }

    /// <summary>
    /// Crea una nueva instancia de lanzamiento musical genérico.
    /// </summary>
    public static MusicRelease Create(
        string title,
        string artist,
        MediaFormat format = MediaFormat.Vinyl,
        Barcode? barcode = null,
        CatalogueNumber? catalogueNumber = null,
        ReleaseYear? releaseYear = null,
        int discsCount = 1,
        VinylSize? vinylSize = null,
        VinylSpeed? vinylSpeed = null,
        string? label = null,
        string? country = null,
        string? coverImageUrl = null,
        string? notes = null)
    {
        ValidateTitle(title);
        ValidateArtist(artist);
        ValidateDiscsCount(discsCount);

        var id = MusicReleaseId.New();
        var now = DateTime.UtcNow;

        // Si es vinilo y no se especificó tamaño/velocidad, asignamos los estándares (12" a 33 RPM)
        var size = format == MediaFormat.Vinyl ? (vinylSize ?? Enums.VinylSize.Inch12) : vinylSize;
        var speed = format == MediaFormat.Vinyl ? (vinylSpeed ?? Enums.VinylSpeed.Rpm33) : vinylSpeed;

        var release = new MusicRelease(
            id,
            format,
            title.Trim(),
            artist.Trim(),
            barcode,
            catalogueNumber,
            releaseYear,
            discsCount,
            size,
            speed,
            label?.Trim(),
            country?.Trim(),
            coverImageUrl?.Trim(),
            notes?.Trim(),
            ProcessingStatus.PendingAnalysis,
            now);

        release.AddDomainEvent(new MusicReleaseCreatedEvent(id, release.Title, release.Artist, format, now));

        return release;
    }

    /// <summary>
    /// Factoría de conveniencia para la creación de un disco de vinilo.
    /// </summary>
    public static MusicRelease CreateVinyl(
        string title,
        string artist,
        VinylSize size = Enums.VinylSize.Inch12,
        VinylSpeed speed = Enums.VinylSpeed.Rpm33,
        int discsCount = 1,
        Barcode? barcode = null,
        CatalogueNumber? catalogueNumber = null,
        ReleaseYear? releaseYear = null,
        string? label = null,
        string? country = null,
        string? coverImageUrl = null,
        string? notes = null)
    {
        return Create(
            title,
            artist,
            MediaFormat.Vinyl,
            barcode,
            catalogueNumber,
            releaseYear,
            discsCount,
            size,
            speed,
            label,
            country,
            coverImageUrl,
            notes);
    }

    /// <summary>
    /// Factoría de conveniencia para la creación de un CD (Compact Disc).
    /// </summary>
    public static MusicRelease CreateCd(
        string title,
        string artist,
        int discsCount = 1,
        Barcode? barcode = null,
        CatalogueNumber? catalogueNumber = null,
        ReleaseYear? releaseYear = null,
        string? label = null,
        string? country = null,
        string? coverImageUrl = null,
        string? notes = null)
    {
        return Create(
            title,
            artist,
            MediaFormat.CD,
            barcode,
            catalogueNumber,
            releaseYear,
            discsCount,
            vinylSize: null,
            vinylSpeed: null,
            label,
            country,
            coverImageUrl,
            notes);
    }

    /// <summary>
    /// Factoría de conveniencia para la creación de un casete de audio.
    /// </summary>
    public static MusicRelease CreateCassette(
        string title,
        string artist,
        int tapesCount = 1,
        Barcode? barcode = null,
        CatalogueNumber? catalogueNumber = null,
        ReleaseYear? releaseYear = null,
        string? label = null,
        string? country = null,
        string? coverImageUrl = null,
        string? notes = null)
    {
        return Create(
            title,
            artist,
            MediaFormat.Cassette,
            barcode,
            catalogueNumber,
            releaseYear,
            tapesCount,
            vinylSize: null,
            vinylSpeed: null,
            label,
            country,
            coverImageUrl,
            notes);
    }

    /// <summary>
    /// Actualiza los metadatos reconocidos tras la extracción visual con IA.
    /// </summary>
    public void UpdateIdentifiedDetails(
        string title,
        string artist,
        Barcode? barcode = null,
        CatalogueNumber? catalogueNumber = null,
        ReleaseYear? releaseYear = null,
        string? label = null,
        string? country = null)
    {
        EnsureNotConfirmed();
        ValidateTitle(title);
        ValidateArtist(artist);

        Title = title.Trim();
        Artist = artist.Trim();
        Barcode = barcode ?? Barcode;
        CatalogueNumber = catalogueNumber ?? CatalogueNumber;
        ReleaseYear = releaseYear ?? ReleaseYear;
        Label = label?.Trim() ?? Label;
        Country = country?.Trim() ?? Country;

        if (Status is ProcessingStatus.PendingAnalysis or ProcessingStatus.Failed)
        {
            Status = ProcessingStatus.Identified;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Enriquece el lanzamiento con información contrastada obtenida de la API canónica (ej. Discogs).
    /// </summary>
    public void EnrichFromDiscogs(
        long discogsReleaseId,
        string? label = null,
        ReleaseYear? releaseYear = null,
        string? country = null,
        string? coverImageUrl = null,
        IEnumerable<Track>? tracks = null)
    {
        EnsureNotConfirmed();

        if (discogsReleaseId <= 0)
        {
            throw new MusicReleaseDomainException("El identificador de release de Discogs debe ser un número positivo.");
        }

        DiscogsReleaseId = discogsReleaseId;
        Label = label?.Trim() ?? Label;
        ReleaseYear = releaseYear ?? ReleaseYear;
        Country = country?.Trim() ?? Country;
        CoverImageUrl = coverImageUrl?.Trim() ?? CoverImageUrl;

        if (tracks is not null)
        {
            _tracks.Clear();
            foreach (var track in tracks)
            {
                AddTrackInternal(track);
            }
        }

        Status = ProcessingStatus.Enriched;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new MusicReleaseEnrichedEvent(Id, discogsReleaseId, _tracks.Count, UpdatedAtUtc.Value));
    }

    /// <summary>
    /// Valida y confirma el lanzamiento para su custodia en la colección personal del usuario.
    /// </summary>
    public void Confirm()
    {
        if (Status == ProcessingStatus.Failed)
        {
            throw new MusicReleaseDomainException("No se puede confirmar un ítem que se encuentra en estado fallido.");
        }

        Status = ProcessingStatus.Confirmed;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new MusicReleaseConfirmedEvent(Id, UpdatedAtUtc.Value));
    }

    /// <summary>
    /// Marca el procesamiento como fallido con un motivo explicativo.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        Status = ProcessingStatus.Failed;
        Notes = string.IsNullOrWhiteSpace(Notes) ? $"Error: {reason.Trim()}" : $"{Notes} | Error: {reason.Trim()}";
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new MusicReleaseFailedEvent(Id, reason.Trim(), UpdatedAtUtc.Value));
    }

    /// <summary>
    /// Asigna o actualiza la graduación del estado físico de conservación del soporte sonoro y su funda/caja.
    /// </summary>
    public void SetGrading(MediaCondition mediaCondition, MediaCondition sleeveCondition)
    {
        MediaCondition = mediaCondition;
        SleeveCondition = sleeveCondition;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Configura las especificaciones físicas si el lanzamiento es un vinilo.
    /// </summary>
    public void SetVinylSpecifications(VinylSize size, VinylSpeed speed, int discsCount = 1)
    {
        if (Format != MediaFormat.Vinyl)
        {
            throw new MusicReleaseDomainException("Solo se pueden configurar especificaciones de vinilo en lanzamientos con formato Vinyl.");
        }

        ValidateDiscsCount(discsCount);

        VinylSize = size;
        VinylSpeed = speed;
        DiscsCount = discsCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza el número de soportes (discos/casetes) incluidos en la edición.
    /// </summary>
    public void SetDiscsCount(int discsCount)
    {
        ValidateDiscsCount(discsCount);
        DiscsCount = discsCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Añade una pista musical asegurando que no existan posiciones duplicadas (ej. dos cortes 'A1' o dos pistas '1').
    /// </summary>
    public void AddTrack(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        AddTrackInternal(track);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Elimina una pista musical por su identificador.
    /// </summary>
    public void RemoveTrack(Guid trackId)
    {
        var removedCount = _tracks.RemoveAll(t => t.Id == trackId);
        if (removedCount > 0)
        {
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Reemplaza o actualiza las notas personales del coleccionista.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void AddTrackInternal(Track track)
    {
        if (_tracks.Any(t => t.Position.Equals(track.Position, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidTrackException($"Ya existe una pista con la posición '{track.Position}' en este lanzamiento musical.");
        }

        _tracks.Add(track);
    }

    private void EnsureNotConfirmed()
    {
        if (Status == ProcessingStatus.Confirmed)
        {
            throw new MusicReleaseDomainException("El lanzamiento ya está confirmado en la colección personal y no puede ser modificado por procesos automáticos.");
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new MusicReleaseDomainException("El título del lanzamiento no puede estar vacío.");
        }
    }

    private static void ValidateArtist(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            throw new MusicReleaseDomainException("El artista del lanzamiento no puede estar vacío.");
        }
    }

    private static void ValidateDiscsCount(int discsCount)
    {
        if (discsCount < 1)
        {
            throw new MusicReleaseDomainException("El número de discos/casetes debe ser como mínimo 1.");
        }
    }
}

