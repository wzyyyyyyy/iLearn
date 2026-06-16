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

        var request = new DownloadRequest(
            Id: "task-1",
            Url: "https://example.test/video.mp4",
            FileName: "video.mp4",
            OutputDirectory: Path.GetTempPath(),
            DisplayName: "第一讲",
            Perspective: "HDMI");

        await service.EnqueueAsync(request, TestContext.Current.CancellationToken);

        var snapshot = Assert.Single(service.Tasks);
        Assert.Equal("task-1", snapshot.Id);
        Assert.Equal(DownloadTaskStatus.Queued, snapshot.Status);
    }

    [Fact]
    public async Task CancelAsync_MarksQueuedTaskCancelled()
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

        await service.CancelAsync("task-1", TestContext.Current.CancellationToken);

        var snapshot = Assert.Single(service.Tasks);
        Assert.Equal(DownloadTaskStatus.Cancelled, snapshot.Status);
    }

    [Fact]
    public async Task EnqueueAsync_RejectsDuplicateTaskId()
    {
        var engine = new FakeDownloadEngine();
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
}
