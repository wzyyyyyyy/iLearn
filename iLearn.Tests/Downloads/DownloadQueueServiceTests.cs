using System.Collections.Specialized;
using iLearn.Downloads;
using Xunit;

namespace iLearn.Tests.Downloads;

public sealed class DownloadQueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_PublishesQueuedSnapshot()
    {
        var engine = new FakeDownloadEngine();
        var service = new DownloadQueueService(engine);
        var statuses = new List<DownloadTaskStatus>();
        ((INotifyCollectionChanged)service.Tasks).CollectionChanged += (_, args) =>
        {
            if (args.NewItems is null)
                return;

            lock (statuses)
            {
                foreach (DownloadTaskSnapshot snapshot in args.NewItems)
                    statuses.Add(snapshot.Status);
            }
        };

        var request = new DownloadRequest(
            Id: "task-1",
            Url: "https://example.test/video.mp4",
            FileName: "video.mp4",
            OutputDirectory: Path.GetTempPath(),
            DisplayName: "第一讲",
            Perspective: "HDMI");

        await service.EnqueueAsync(request, TestContext.Current.CancellationToken);
        await WaitUntilAsync(() =>
        {
            lock (statuses)
                return statuses.Contains(DownloadTaskStatus.Completed);
        });

        var snapshot = Assert.Single(service.Tasks);
        Assert.Equal("task-1", snapshot.Id);
        lock (statuses)
            Assert.Contains(DownloadTaskStatus.Queued, statuses);
    }

    [Fact]
    public async Task CancelAsync_RemovesActiveTaskFromQueue()
    {
        var engine = new BlockingDownloadEngine();
        var service = new DownloadQueueService(engine);

        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"),
            TestContext.Current.CancellationToken);

        await service.CancelAsync("task-1", TestContext.Current.CancellationToken);

        Assert.Empty(service.Tasks);
    }

    [Fact]
    public async Task EnqueueAsync_RejectsDuplicateTaskId()
    {
        var engine = new BlockingDownloadEngine();
        var service = new DownloadQueueService(engine);
        var request = new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI");

        await service.EnqueueAsync(request, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnqueueAsync(request, TestContext.Current.CancellationToken));

        await service.CancelAsync("task-1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CancelAsync_DoesNotOverwriteCompletedTask()
    {
        var engine = new FakeDownloadEngine();
        var service = new DownloadQueueService(engine);

        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => service.Tasks.Single().Status == DownloadTaskStatus.Completed);

        await service.CancelAsync("task-1", TestContext.Current.CancellationToken);

        Assert.Equal(DownloadTaskStatus.Completed, service.Tasks.Single().Status);
    }

    [Fact]
    public async Task RetryAsync_RejectsWhileCancelledRunIsStillStopping()
    {
        var engine = new SlowCancellingDownloadEngine();
        var service = new DownloadQueueService(engine);
        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => service.Tasks.Single().Status == DownloadTaskStatus.Downloading);

        await service.CancelAsync("task-1", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetryAsync("task-1", TestContext.Current.CancellationToken));

        engine.AllowCancellationToFinish();
        await WaitUntilAsync(() => service.Tasks.Count == 0);
    }

    [Fact]
    public async Task RetryAsync_RejectsCancelledTaskAfterItWasRemoved()
    {
        var engine = new BlockingDownloadEngine();
        var service = new DownloadQueueService(engine);
        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"),
            TestContext.Current.CancellationToken);

        await service.CancelAsync("task-1", TestContext.Current.CancellationToken);
        Assert.Empty(service.Tasks);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetryAsync("task-1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClearCompleted_RemovesCompletedTasks()
    {
        var engine = new FakeDownloadEngine();
        var service = new DownloadQueueService(engine);
        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => service.Tasks.Single().Status == DownloadTaskStatus.Completed);

        service.ClearCompleted();

        Assert.Empty(service.Tasks);
    }

    [Fact]
    public async Task RetryFailedAsync_RequeuesFailedTasks()
    {
        var engine = new FailingDownloadEngine();
        var service = new DownloadQueueService(engine);
        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"),
            TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => service.Tasks.Single().Status == DownloadTaskStatus.Failed);

        engine.ShouldFail = false;
        await service.RetryFailedAsync(TestContext.Current.CancellationToken);

        await WaitUntilAsync(() => service.Tasks.Single().Status == DownloadTaskStatus.Completed);
    }

    private sealed class FakeDownloadEngine : IDownloadEngine
    {
        public Task DownloadAsync(
            DownloadRequest request,
            string outputPath,
            IProgress<DownloadTaskSnapshot> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(DownloadTaskSnapshot.Downloading(request, 10, 100, 1024, null));
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingDownloadEngine : IDownloadEngine
    {
        public async Task DownloadAsync(
            DownloadRequest request,
            string outputPath,
            IProgress<DownloadTaskSnapshot> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(DownloadTaskSnapshot.Downloading(request, 10, 100, 1024, null));
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class SlowCancellingDownloadEngine : IDownloadEngine
    {
        private readonly TaskCompletionSource _allowCancellationToFinish = new();

        public async Task DownloadAsync(
            DownloadRequest request,
            string outputPath,
            IProgress<DownloadTaskSnapshot> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(DownloadTaskSnapshot.Downloading(request, 10, 100, 1024, null));
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await _allowCancellationToFinish.Task.WaitAsync(TestContext.Current.CancellationToken);
                throw;
            }
        }

        public void AllowCancellationToFinish()
        {
            _allowCancellationToFinish.SetResult();
        }
    }

    private sealed class FailingDownloadEngine : IDownloadEngine
    {
        public bool ShouldFail { get; set; } = true;

        public Task DownloadAsync(
            DownloadRequest request,
            string outputPath,
            IProgress<DownloadTaskSnapshot> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(DownloadTaskSnapshot.Downloading(request, 10, 100, 1024, null));
            if (ShouldFail)
                throw new InvalidOperationException("network failed");
            return Task.CompletedTask;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
