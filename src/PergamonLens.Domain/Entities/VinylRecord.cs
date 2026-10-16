using PergamonLens.Domain.Common;
using PergamonLens.Domain.Enums;
using PergamonLens.Domain.Events;
using PergamonLens.Domain.Exceptions;
using PergamonLens.Domain.ValueObjects;

namespace PergamonLens.Domain.Entities;

/// <summary>
/// Raíz de agregado (Aggregate Root) que representa un disco de vinilo físico en la colección.
/// Encapsula sus metadatos discográficos, especificaciones físicas, estado de conservación y pistas.
/// </summary>
public sealed class VinylRecord : AggregateRoot<VinylRecordId>
{
    private readonly List<Track> _tracks = [];

    public string Title { get; private set; } = string.Empty;
    public string Artist { get; private set; } = string.Empty;
    public string? Label { get; private set; }
    public ReleaseYear? ReleaseYear { get; private set; }
    public Barcode? Barcode { get; private set; }
    public CatalogueNumber? CatalogueNumber { get; private set; }
    public string? Country { get; private set; }
    public string? CoverImageUrl { get; private set; }

    public VinylSize Size { get; private set; } = VinylSize.Inch12;
    public VinylSpeed Speed { get; private set; } = VinylSpeed.Rpm33;
    public int DiscsCount { get; private set; } = 1;

    public MediaCondition? MediaCondition { get; private set; }
    public MediaCondition? SleeveCondition { get; private set; }

    public ProcessingStatus Status { get; private set; } = ProcessingStatus.PendingAnalysis;
    public long? DiscogsReleaseId { get; private set; }
    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<Track> Tracks => _tracks.AsReadOnly();

    private VinylRecord(
        VinylRecordId id,
        string title,
        string artist,
        Barcode? barcode,
        CatalogueNumber? catalogueNumber,
        ReleaseYear? releaseYear,
        VinylSize size,
        VinylSpeed speed,
        int discsCount,
        string? label,
        string? country,
        string? coverImageUrl,
        string? notes,
        ProcessingStatus status,
        DateTime createdAtUtc) : base(id)
    {
        Title = title;
        Artist = artist;
        Barcode = barcode;
        CatalogueNumber = catalogueNumber;
        ReleaseYear = releaseYear;
        Size = size;
        Speed = speed;
        DiscsCount = discsCount;
        Label = label;
        Country = country;
        CoverImageUrl = coverImageUrl;
        Notes = notes;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    // Constructor privado requerido por EF Core
    private VinylRecord() { }

    /// <summary>
    /// Crea una nueva instancia de vinilo en estado pendiente de análisis o pre-identificado.
    /// </summary>
    public static VinylRecord Create(
        string title,
        string artist,
        Barcode? barcode = null,
        CatalogueNumber? catalogueNumber = null,
        ReleaseYear? releaseYear = null,
        VinylSize size = VinylSize.Inch12,
        VinylSpeed speed = VinylSpeed.Rpm33,
        int discsCount = 1,
        string? label = null,
        string? country = null,
        string? coverImageUrl = null,
        string? notes = null)
    {
        ValidateTitle(title);
        ValidateArtist(artist);
        ValidateDiscsCount(discsCount);

        var id = VinylRecordId.New();
        var now = DateTime.UtcNow;

        var vinyl = new VinylRecord(
            id,
            title.Trim(),
            artist.Trim(),
            barcode,
            catalogueNumber,
            releaseYear,
            size,
            speed,
            discsCount,
            label?.Trim(),
            country?.Trim(),
            coverImageUrl?.Trim(),
            notes?.Trim(),
            ProcessingStatus.PendingAnalysis,
            now);

        vinyl.AddDomainEvent(new VinylRecordCreatedEvent(id, vinyl.Title, vinyl.Artist, now));

        return vinyl;
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
    /// Enriquece el vinilo con información contrastada obtenida de la API canónica (ej. Discogs).
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
            throw new VinylRecordDomainException("El identificador de release de Discogs debe ser un número positivo.");
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

        AddDomainEvent(new VinylRecordEnrichedEvent(Id, discogsReleaseId, _tracks.Count, UpdatedAtUtc.Value));
    }

    /// <summary>
    /// Valida y confirma el vinilo para su preservación en la bóveda personal del coleccionista.
    /// </summary>
    public void Confirm()
    {
        if (Status == ProcessingStatus.Failed)
        {
            throw new VinylRecordDomainException("No se puede confirmar un vinilo que se encuentra en estado fallido.");
        }

        Status = ProcessingStatus.Confirmed;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(new VinylRecordConfirmedEvent(Id, UpdatedAtUtc.Value));
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

        AddDomainEvent(new VinylRecordFailedEvent(Id, reason.Trim(), UpdatedAtUtc.Value));
    }

    /// <summary>
    /// Asigna o actualiza la graduación del estado físico de conservación del vinilo y su cubierta.
    /// </summary>
    public void SetGrading(MediaCondition mediaCondition, MediaCondition sleeveCondition)
    {
        MediaCondition = mediaCondition;
        SleeveCondition = sleeveCondition;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Actualiza el formato físico (dimensiones, velocidad y número de discos).
    /// </summary>
    public void SetPhysicalFormat(VinylSize size, VinylSpeed speed, int discsCount)
    {
        ValidateDiscsCount(discsCount);

        Size = size;
        Speed = speed;
        DiscsCount = discsCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Añade una pista musical asegurando que no existan posiciones duplicadas (ej. dos cortes 'A1').
    /// </summary>
    public void AddTrack(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);
        AddTrackInternal(track);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Elimina una pista del vinilo por su identificador.
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
            throw new InvalidTrackException($"Ya existe una pista con la posición '{track.Position}' en este vinilo.");
        }

        _tracks.Add(track);
    }

    private void EnsureNotConfirmed()
    {
        if (Status == ProcessingStatus.Confirmed)
        {
            throw new VinylRecordDomainException("El vinilo ya está confirmado en la colección personal y no puede ser modificado por procesos automáticos.");
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new VinylRecordDomainException("El título del vinilo no puede estar vacío.");
        }
    }

    private static void ValidateArtist(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            throw new VinylRecordDomainException("El artista del vinilo no puede estar vacío.");
        }
    }

    private static void ValidateDiscsCount(int discsCount)
    {
        if (discsCount < 1)
        {
            throw new VinylRecordDomainException("El número de discos debe ser como mínimo 1.");
        }
    }
}
