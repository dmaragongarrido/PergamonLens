using FluentAssertions;
using PergamonLens.Infrastructure.BackgroundServices.ChannelQueue;

namespace PergamonLens.UnitTests.Infrastructure;

public class ReleaseScanQueueTests
{
    [Fact]
    public async Task QueueScanAsync_ShouldEnqueueAndAllowDequeueInOrder()
    {
        // Arrange
        var queue = new ReleaseScanQueue(capacity: 10);
        var releaseId1 = Guid.NewGuid();
        var releaseId2 = Guid.NewGuid();

        var task1 = new ReleaseScanTask(releaseId1, [0x01, 0x02], "image/jpeg", DateTime.UtcNow);
        var task2 = new ReleaseScanTask(releaseId2, [0x03, 0x04], "image/png", DateTime.UtcNow);

        // Act
        await queue.QueueScanAsync(task1);
        await queue.QueueScanAsync(task2);

        var dequeuedTasks = new List<ReleaseScanTask>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await foreach (var item in queue.DequeueAllAsync(cts.Token))
        {
            dequeuedTasks.Add(item);
            if (dequeuedTasks.Count == 2)
            {
                break;
            }
        }

        // Assert
        dequeuedTasks.Should().HaveCount(2);
        dequeuedTasks[0].ReleaseId.Should().Be(releaseId1);
        dequeuedTasks[1].ReleaseId.Should().Be(releaseId2);
    }
}
