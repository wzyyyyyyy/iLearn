using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using iLearn.Models;

namespace iLearn.Downloads;

public sealed class HttpRangeDownloadEngine : IDownloadEngine
{
    private const int BufferSize = 128 * 1024;

    private readonly HttpClient _httpClient;
    private readonly AppConfig _appConfig;

    public HttpRangeDownloadEngine()
        : this(new HttpClient { Timeout = TimeSpan.FromMinutes(10) }, new AppConfig())
    {
    }

    public HttpRangeDownloadEngine(HttpClient httpClient)
        : this(httpClient, new AppConfig())
    {
    }

    public HttpRangeDownloadEngine(HttpClient httpClient, AppConfig appConfig)
    {
        _httpClient = httpClient;
        _appConfig = appConfig;
    }

    public async Task DownloadAsync(
        DownloadRequest request,
        string outputPath,
        IProgress<DownloadTaskSnapshot> progress,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".part";

        try
        {
            var limiter = new ConfiguredSpeedLimiter(_appConfig);
            var totalBytes = GetChunkCount() > 1
                ? await TryGetRangeDownloadLengthAsync(request.Url, cancellationToken)
                : null;

            if (totalBytes is > 0)
                await DownloadInChunksAsync(request, tempPath, totalBytes.Value, progress, limiter, cancellationToken);
            else
                await DownloadSequentiallyAsync(request, tempPath, progress, limiter, cancellationToken);

            File.Move(tempPath, outputPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    private async Task DownloadSequentiallyAsync(
        DownloadRequest request,
        string tempPath,
        IProgress<DownloadTaskSnapshot> progress,
        ConfiguredSpeedLimiter limiter,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            request.Url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        long bytesReceived = 0;
        var stopwatch = Stopwatch.StartNew();
        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = File.Create(tempPath))
        {
            var buffer = new byte[BufferSize];
            var lastReport = TimeSpan.Zero;

            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesReceived += read;
                await limiter.ThrottleAsync(read, cancellationToken);

                if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(250))
                {
                    lastReport = stopwatch.Elapsed;
                    progress.Report(CreateProgressSnapshot(request, bytesReceived, totalBytes, stopwatch));
                }
            }

            if (totalBytes > 0 && bytesReceived != totalBytes)
                throw new EndOfStreamException($"Download ended after {bytesReceived} bytes, expected {totalBytes} bytes.");

            progress.Report(CreateProgressSnapshot(request, bytesReceived, totalBytes, stopwatch));
        }
    }

    private async Task DownloadInChunksAsync(
        DownloadRequest request,
        string tempPath,
        long totalBytes,
        IProgress<DownloadTaskSnapshot> progress,
        ConfiguredSpeedLimiter limiter,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var reportGate = new object();
        var lastReport = TimeSpan.Zero;
        long bytesReceived = 0;

        await using (var output = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.RandomAccess))
        {
            output.SetLength(totalBytes);
        }

        await Task.WhenAll(CreateRanges(totalBytes).Select(range =>
            DownloadChunkAsync(range.Start, range.End)));

        if (bytesReceived != totalBytes)
            throw new EndOfStreamException($"Download ended after {bytesReceived} bytes, expected {totalBytes} bytes.");

        progress.Report(CreateProgressSnapshot(request, bytesReceived, totalBytes, stopwatch));

        async Task DownloadChunkAsync(long start, long end)
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, request.Url);
            message.Headers.Range = new RangeHeaderValue(start, end);
            using var response = await _httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode != HttpStatusCode.PartialContent)
                throw new InvalidOperationException("Server did not honor the requested byte range.");

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(
                tempPath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.ReadWrite,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess);
            output.Seek(start, SeekOrigin.Begin);

            var buffer = new byte[BufferSize];
            var remaining = end - start + 1;
            while (remaining > 0)
            {
                var read = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
                if (read == 0)
                    throw new EndOfStreamException($"Range {start}-{end} ended with {remaining} bytes remaining.");

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                remaining -= read;
                var totalReceived = Interlocked.Add(ref bytesReceived, read);
                await limiter.ThrottleAsync(read, cancellationToken);
                ReportIfNeeded(totalReceived);
            }
        }

        void ReportIfNeeded(long totalReceived)
        {
            lock (reportGate)
            {
                if (stopwatch.Elapsed - lastReport < TimeSpan.FromMilliseconds(250))
                    return;

                lastReport = stopwatch.Elapsed;
            }

            progress.Report(CreateProgressSnapshot(request, totalReceived, totalBytes, stopwatch));
        }
    }

    private async Task<long?> TryGetRangeDownloadLengthAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Range = new RangeHeaderValue(0, 0);
            using var response = await _httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.StatusCode == HttpStatusCode.PartialContent
                ? response.Content.Headers.ContentRange?.Length
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private IEnumerable<(long Start, long End)> CreateRanges(long totalBytes)
    {
        var chunkCount = Math.Min(GetChunkCount(), (int)Math.Min(totalBytes, int.MaxValue));
        if (chunkCount <= 1)
        {
            yield return (0, totalBytes - 1);
            yield break;
        }

        var chunkSize = totalBytes / chunkCount;
        for (var index = 0; index < chunkCount; index++)
        {
            var start = index * chunkSize;
            var end = index == chunkCount - 1
                ? totalBytes - 1
                : start + chunkSize - 1;
            yield return (start, end);
        }
    }

    private int GetChunkCount()
    {
        return Math.Clamp(_appConfig.ChunkCount, 1, 32);
    }

    private static DownloadTaskSnapshot CreateProgressSnapshot(
        DownloadRequest request,
        long bytesReceived,
        long totalBytes,
        Stopwatch stopwatch)
    {
        return DownloadTaskSnapshot.Downloading(
            request,
            bytesReceived,
            totalBytes,
            bytesReceived / Math.Max(1, stopwatch.Elapsed.TotalSeconds),
            null);
    }

    private sealed class ConfiguredSpeedLimiter
    {
        private readonly AppConfig _appConfig;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private long _bytesReceived;

        public ConfiguredSpeedLimiter(AppConfig appConfig)
        {
            _appConfig = appConfig;
        }

        public async Task ThrottleAsync(int bytesReceived, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                _bytesReceived += bytesReceived;
                var limit = _appConfig.SpeedLimitBytesPerSecond;
                if (limit <= 0)
                    return;

                var expectedElapsed = TimeSpan.FromSeconds(_bytesReceived / (double)limit);
                var delay = expectedElapsed - _stopwatch.Elapsed;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
