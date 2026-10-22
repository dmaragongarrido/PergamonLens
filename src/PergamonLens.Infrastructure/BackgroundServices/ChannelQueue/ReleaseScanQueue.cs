using System.Threading.Channels;

namespace PergamonLens.Infrastructure.BackgroundServices.ChannelQueue;

/// <summary>
/// Cola asíncrona de alto rendimiento implementada mediante un canal acotado (<see cref="Channel{T}"/>).
/// Garantiza contrapresión (backpressure) y control estricto de concurrencia en memoria.
/// </summary>
public sealed class ReleaseScanQueue : IReleaseScanQueue
{
    private readonly Channel<ReleaseScanTask> _channel;

    public ReleaseScanQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ReleaseScanTask>(options);
    }

    public async ValueTask QueueScanAsync(ReleaseScanTask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        await _channel.Writer.WriteAsync(task, cancellationToken);
    }

    public IAsyncEnumerable<ReleaseScanTask> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
