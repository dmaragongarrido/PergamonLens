namespace PergamonLens.Infrastructure.BackgroundServices.ChannelQueue;

/// <summary>
/// Elemento de trabajo para el procesamiento visual en segundo plano.
/// </summary>
public sealed record ReleaseScanTask(
    Guid ReleaseId,
    byte[] ImageData,
    string ContentType,
    DateTime QueuedAtUtc);

/// <summary>
/// Contrato para la cola concurrente en segundo plano basada en System.Threading.Channels.
/// </summary>
public interface IReleaseScanQueue
{
    ValueTask QueueScanAsync(ReleaseScanTask task, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ReleaseScanTask> DequeueAllAsync(CancellationToken cancellationToken = default);
}
