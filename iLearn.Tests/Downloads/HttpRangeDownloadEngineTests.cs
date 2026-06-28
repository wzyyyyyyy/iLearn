using iLearn.Downloads;
using iLearn.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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

    [Fact]
    public async Task DownloadAsync_UsesConfiguredSpeedLimit()
    {
        var source = Enumerable.Range(0, 8 * 1024).Select(index => (byte)(index % 256)).ToArray();
        var handler = new StaticBytesHandler(source);
        var config = new AppConfig { SpeedLimitBytesPerSecond = 32 * 1024 };
        var engine = new HttpRangeDownloadEngine(new HttpClient(handler), config);
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-download-tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(directory, "video.mp4");
        var request = new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            directory,
            "第一讲",
            "HDMI");

        var stopwatch = Stopwatch.StartNew();
        await engine.DownloadAsync(
            request,
            outputPath,
            new InlineProgress(_ => { }),
            TestContext.Current.CancellationToken);

        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public async Task DownloadAsync_UsesRangeRequestsWhenChunkCountIsConfigured()
    {
        var source = Enumerable.Range(0, 256 * 1024).Select(index => (byte)(index % 256)).ToArray();
        var handler = new RangeAwareBytesHandler(source);
        var config = new AppConfig { ChunkCount = 4 };
        var engine = new HttpRangeDownloadEngine(new HttpClient(handler), config);
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-download-tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(directory, "video.mp4");
        var request = new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            directory,
            "第一讲",
            "HDMI");

        await engine.DownloadAsync(
            request,
            outputPath,
            new InlineProgress(_ => { }),
            TestContext.Current.CancellationToken);

        Assert.Equal(source, await File.ReadAllBytesAsync(outputPath, TestContext.Current.CancellationToken));
        Assert.Contains("bytes=0-0", handler.RangeHeaders);
        Assert.True(handler.RangeHeaders.Count(header => header is not null && header != "bytes=0-0") >= 4);
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

    private sealed class RangeAwareBytesHandler : HttpMessageHandler
    {
        private readonly byte[] _bytes;

        public RangeAwareBytesHandler(byte[] bytes)
        {
            _bytes = bytes;
        }

        public List<string?> RangeHeaders { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var range = request.Headers.Range;
            RangeHeaders.Add(range?.ToString());

            if (range?.Ranges.SingleOrDefault() is not { } rangeItem)
            {
                var fullResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_bytes)
                };
                fullResponse.Content.Headers.ContentLength = _bytes.Length;
                return Task.FromResult(fullResponse);
            }

            var start = rangeItem.From ?? 0;
            var end = rangeItem.To ?? _bytes.Length - 1;
            var length = (int)(end - start + 1);
            var body = _bytes.Skip((int)start).Take(length).ToArray();
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
            {
                Content = new ByteArrayContent(body)
            };
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, _bytes.Length);
            response.Content.Headers.ContentLength = body.Length;
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
