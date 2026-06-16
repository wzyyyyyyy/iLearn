using iLearn.Downloads;
using Xunit;

namespace iLearn.Tests.Downloads;

public sealed class HttpRangeDownloadEngineTests
{
    [Fact]
    public async Task DownloadAsync_WritesFile_AndReportsCompletionBytes()
    {
        var source = new byte[] { 1, 2, 3, 4, 5 };
        var handler = new StaticBytesHandler(source);
        var engine = new HttpRangeDownloadEngine(new HttpClient(handler));
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-download-tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(directory, "video.mp4");
        var request = new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            directory,
            "第一讲",
            "HDMI");
        DownloadTaskSnapshot? last = null;

        await engine.DownloadAsync(
            request,
            outputPath,
            new InlineProgress(snapshot => last = snapshot),
            TestContext.Current.CancellationToken);

        Assert.Equal(source, await File.ReadAllBytesAsync(outputPath, TestContext.Current.CancellationToken));
        Assert.NotNull(last);
        Assert.Equal(source.Length, last!.BytesReceived);
    }

    [Fact]
    public async Task DownloadAsync_DoesNotLeaveFinalFile_WhenResponseIsTruncated()
    {
        var handler = new StaticBytesHandler(new byte[] { 1, 2, 3 }, contentLength: 10);
        var engine = new HttpRangeDownloadEngine(new HttpClient(handler));
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-download-tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(directory, "video.mp4");
        var request = new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            directory,
            "第一讲",
            "HDMI");

        await Assert.ThrowsAsync<EndOfStreamException>(() =>
            engine.DownloadAsync(
                request,
                outputPath,
                new InlineProgress(_ => { }),
                TestContext.Current.CancellationToken));

        Assert.False(File.Exists(outputPath));
    }

    private sealed class StaticBytesHandler : HttpMessageHandler
    {
        private readonly byte[] _bytes;
        private readonly long? _contentLength;

        public StaticBytesHandler(byte[] bytes, long? contentLength = null)
        {
            _bytes = bytes;
            _contentLength = contentLength;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_bytes)
            };
            response.Content.Headers.ContentLength = _contentLength ?? _bytes.Length;
            return Task.FromResult(response);
        }
    }

    private sealed class InlineProgress : IProgress<DownloadTaskSnapshot>
    {
        private readonly Action<DownloadTaskSnapshot> _report;

        public InlineProgress(Action<DownloadTaskSnapshot> report)
        {
            _report = report;
        }

        public void Report(DownloadTaskSnapshot value)
        {
            _report(value);
        }
    }
}
