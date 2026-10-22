using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PergamonLens.Domain.Repositories;
using PergamonLens.Domain.Services;
using PergamonLens.Domain.ValueObjects;
using PergamonLens.Infrastructure.BackgroundServices.ChannelQueue;

namespace PergamonLens.Infrastructure.BackgroundServices;

/// <summary>
/// Worker asíncrono en segundo plano que procesa los análisis visuales encolados
/// sin bloquear el hilo principal de peticiones HTTP.
/// </summary>
public sealed class ReleaseScanBackgroundWorker : BackgroundService
{
    private readonly IReleaseScanQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReleaseScanBackgroundWorker> _logger;

    public ReleaseScanBackgroundWorker(
        IReleaseScanQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ReleaseScanBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReleaseScanBackgroundWorker iniciado y escuchando el canal de escaneos...");

        await foreach (var task in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Procesando tarea de escaneo para Release ID: {ReleaseId}", task.ReleaseId);

                using var scope = _scopeFactory.CreateScope();
                var visionService = scope.ServiceProvider.GetRequiredService<IVisionService>();
                var repository = scope.ServiceProvider.GetRequiredService<IMusicReleaseRepository>();

                var releaseId = new MusicReleaseId(task.ReleaseId);
                var release = await repository.GetByIdAsync(releaseId, stoppingToken);

                if (release is null)
                {
                    _logger.LogWarning("Release {ReleaseId} no encontrada en la base de datos al procesar la cola", task.ReleaseId);
                    continue;
                }

                using var memoryStream = new MemoryStream(task.ImageData);
                var extraction = await visionService.AnalyzeImageAsync(memoryStream, task.ContentType, stoppingToken);

                if (!string.IsNullOrWhiteSpace(extraction.DetectedTitle) && !string.IsNullOrWhiteSpace(extraction.DetectedArtist))
                {
                    Barcode? barcode = null;
                    if (!string.IsNullOrWhiteSpace(extraction.DetectedBarcode) &&
                        Barcode.TryCreate(extraction.DetectedBarcode, out var b, out _))
                    {
                        barcode = b;
                    }

                    CatalogueNumber? catNo = null;
                    if (!string.IsNullOrWhiteSpace(extraction.DetectedCatalogueNumber) &&
                        CatalogueNumber.TryCreate(extraction.DetectedCatalogueNumber, out var c, out _))
                    {
                        catNo = c;
                    }

                    release.UpdateIdentifiedDetails(
                        extraction.DetectedTitle,
                        extraction.DetectedArtist,
                        barcode,
                        catNo,
                        label: extraction.DetectedLabel);

                    await repository.UpdateAsync(release, stoppingToken);
                    _logger.LogInformation("Release {ReleaseId} identificada exitosamente como '{Title}' de '{Artist}'",
                        task.ReleaseId, extraction.DetectedTitle, extraction.DetectedArtist);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error no controlado al procesar tarea de escaneo para Release {ReleaseId}", task.ReleaseId);
            }
        }

        _logger.LogInformation("ReleaseScanBackgroundWorker detenido.");
    }
}
