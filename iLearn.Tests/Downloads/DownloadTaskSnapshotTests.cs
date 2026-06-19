using iLearn.Downloads;
using Xunit;

namespace iLearn.Tests.Downloads;

public sealed class DownloadTaskSnapshotTests
{
    [Fact]
    public void DownloadingSnapshot_FormatsStatusSpeedSizeAndRemainingTime()
    {
        var request = CreateRequest();

        var snapshot = DownloadTaskSnapshot.Downloading(
            request,
            bytesReceived: 512 * 1024,
            totalBytes: 1024 * 1024,
            bytesPerSecond: 256 * 1024,
            errorMessage: null);

        Assert.Equal("下载中", snapshot.StatusText);
        Assert.Equal("256 KB/s", snapshot.SpeedText);
        Assert.Equal("512 KB / 1 MB", snapshot.SizeText);
        Assert.Equal("00:02", snapshot.RemainingText);
    }

    [Fact]
    public void QueuedSnapshot_FormatsStableStatusAndIdleSpeed()
    {
        var snapshot = DownloadTaskSnapshot.FromRequest(CreateRequest(), DownloadTaskStatus.Queued);

        Assert.Equal("排队中", snapshot.StatusText);
        Assert.Equal("--", snapshot.SpeedText);
        Assert.Equal("0 B", snapshot.SizeText);
        Assert.Equal("--", snapshot.RemainingText);
    }

    private static DownloadRequest CreateRequest()
    {
        return new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI");
    }
}
