using System.Diagnostics;

namespace iLearn.Downloads;

public sealed class HttpRangeDownloadEngine : IDownloadEngine
{
    private readonly HttpClient _httpClient;

    public HttpRangeDownloadEngine()
        : this(new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
    {
    }

    public HttpRangeDownloadEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
                var buffer = new byte[128 * 1024];
                var lastReport = TimeSpan.Zero;

                while (true)
                {
                    var read = await input.ReadAsync(buffer, cancellationToken);
                    if (read == 0)
                        break;

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    bytesReceived += read;

                    if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(250))
                    {
                        lastReport = stopwatch.Elapsed;
                        progress.Report(DownloadTaskSnapshot.Downloading(
                            request,
                            bytesReceived,
                            totalBytes,
                            bytesReceived / Math.Max(1, stopwatch.Elapsed.TotalSeconds),
                            null));
                    }
                }

                if (totalBytes > 0 && bytesReceived != totalBytes)
                    throw new EndOfStreamException($"Download ended after {bytesReceived} bytes, expected {totalBytes} bytes.");

                progress.Report(DownloadTaskSnapshot.Downloading(
                    request,
                    bytesReceived,
                    totalBytes,
                    bytesReceived / Math.Max(1, stopwatch.Elapsed.TotalSeconds),
                    null));
            }

            File.Move(tempPath, outputPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }
}
