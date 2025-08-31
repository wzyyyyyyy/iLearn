using Downloader;
using iLearn.Models;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace iLearn.Services
{
    public class VideoDownloadService : IDisposable
    {
        public IEnumerable<DownloadItem> ActiveDownloads => _activeDownloads.Values;

        private readonly AppConfig _appConfig;
        private readonly ConcurrentDictionary<string, DownloadItem> _activeDownloads = new();
        private readonly ConcurrentDictionary<string, DownloadService> _downloaders = new();
        private readonly ConcurrentQueue<DownloadRequest> _downloadQueue = new();
        private readonly SemaphoreSlim _queueProcessingSemaphore = new(1, 1); // 确保队列处理不重复
        private SemaphoreSlim _concurrencyLimitSemaphore;
        private int _maxConcurrentDownloads;
        private readonly DownloadConfiguration _config;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private volatile bool _disposed = false;

        public VideoDownloadService(AppConfig appConfig)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _maxConcurrentDownloads = Math.Max(1, _appConfig.MaxConcurrentDownloads);

            _config = new DownloadConfiguration
            {
                ChunkCount = Math.Max(1, _appConfig.ChunkCount),
                ParallelDownload = true,
                Timeout = 600000,
                BufferBlockSize = 10240,
                MaxTryAgainOnFailover = 20,
                MaximumBytesPerSecond = Math.Max(0, _appConfig.SpeedLimitBytesPerSecond),
                RequestConfiguration = new RequestConfiguration
                {
                    Accept = "*/*",
                    Timeout = 200000
                }
            };

            _concurrencyLimitSemaphore = new SemaphoreSlim(_maxConcurrentDownloads, _maxConcurrentDownloads);
        }

        public async Task<bool> StartDownloadAsync(string url, string fileName, string outputPath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VideoDownloadService));

            if (_activeDownloads.ContainsKey(url))
                return false;

            string fullPath = Path.Combine(outputPath, fileName);

            var item = new DownloadItem
            {
                Url = url,
                FileName = fileName,
                OutputPath = fullPath,
                Status = "Waiting",
                Speed = "0 KB/s",
                SpeedValue = 0
            };

            _activeDownloads[url] = item;

            var downloadRequest = new DownloadRequest(url, fileName, outputPath);
            _downloadQueue.Enqueue(downloadRequest);

            _ = Task.Run(() => ProcessDownloadQueueAsync(_cancellationTokenSource.Token));

            return true;
        }

        private async Task ProcessDownloadQueueAsync(CancellationToken cancellationToken)
        {
            if (_disposed) return;

            if (!await _queueProcessingSemaphore.WaitAsync(0, cancellationToken))
                return;

            try
            {
                while (_downloadQueue.TryDequeue(out var downloadRequest) && !cancellationToken.IsCancellationRequested)
                {
                    if (!_activeDownloads.TryGetValue(downloadRequest.Url, out var item))
                        continue;

                    item.Status = "Queued";

                    await _concurrencyLimitSemaphore.WaitAsync(cancellationToken);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await StartActualDownloadAsync(downloadRequest.Url, downloadRequest.FileName, downloadRequest.OutputPath, cancellationToken);
                        }
                        finally
                        {
                            _concurrencyLimitSemaphore.Release();
                        }
                    }, cancellationToken);
                }
            }
            finally
            {
                _queueProcessingSemaphore.Release();
            }
        }

        private async Task StartActualDownloadAsync(string url, string fileName, string outputPath, CancellationToken cancellationToken)
        {
            if (!_activeDownloads.TryGetValue(url, out var item))
                return;

            string fullPath = Path.Combine(outputPath, fileName);
            DownloadService? downloader = null;

            try
            {
                downloader = new DownloadService(_config);
                _downloaders[url] = downloader;

                downloader.DownloadProgressChanged += (s, e) => UpdateProgress(item, e);
                downloader.DownloadFileCompleted += (s, e) => OnDownloadCompleted(url, item, e);

                item.Status = "Downloading";
                await downloader.DownloadFileTaskAsync(url, fullPath, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                item.Status = "Cancelled";
            }
            catch (Exception ex)
            {
                item.Status = "Failed";
            }
            finally
            {
                if (downloader != null)
                {
                    _downloaders.TryRemove(url, out _);
                    downloader.Dispose();
                }

                ResetItemSpeed(item);
            }
        }

        private void UpdateProgress(DownloadItem item, DownloadProgressChangedEventArgs e)
        {
            if (_disposed) return;

            item.Progress = e.ProgressPercentage;
            item.BytesReceived = e.ReceivedBytesSize;
            item.TotalBytes = e.TotalBytesToReceive;

            var speedKB = e.BytesPerSecondSpeed / 1024.0;
            item.Speed = speedKB switch
            {
                >= 1024 => $"{speedKB / 1024.0:F2} MB/s",
                >= 1 => $"{speedKB:F2} KB/s",
                _ => $"{e.BytesPerSecondSpeed:F0} B/s"
            };
            item.SpeedValue = e.BytesPerSecondSpeed;
        }

        private void OnDownloadCompleted(string url, DownloadItem item, AsyncCompletedEventArgs e)
        {
            if (_disposed) return;

            if (e.Cancelled)
                item.Status = "Cancelled";
            else if (e.Error != null)
                item.Status = "Failed";
            else
                item.Status = "Completed";

            ResetItemSpeed(item);
        }

        private static void ResetItemSpeed(DownloadItem item)
        {
            item.Speed = "0 KB/s";
            item.SpeedValue = 0;
        }

        public bool PauseDownload(string url)
        {
            if (_disposed || !_downloaders.TryGetValue(url, out var downloader))
                return false;

            try
            {
                downloader.Pause();

                if (_activeDownloads.TryGetValue(url, out var item))
                {
                    item.Status = "Paused";
                    ResetItemSpeed(item);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ResumeDownload(string url)
        {
            if (_disposed || !_downloaders.TryGetValue(url, out var downloader))
                return false;

            try
            {
                downloader.Resume();

                if (_activeDownloads.TryGetValue(url, out var item))
                    item.Status = "Downloading";

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CancelDownload(string url)
        {
            if (_disposed)
                return false;

            try
            {
                if (_downloaders.TryGetValue(url, out var downloader))
                {
                    downloader.CancelAsync();
                    _downloaders.TryRemove(url, out _);
                }

                if (_activeDownloads.TryGetValue(url, out var item))
                {
                    item.Status = "Cancelled";
                    ResetItemSpeed(item);
                }

                Task.Delay(1000).ContinueWith((_)=>File.Delete(item.OutputPath));
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
        }

        public async Task<bool> RetryDownloadAsync(string url)
        {
            if (_disposed || !_activeDownloads.TryGetValue(url, out var item))
                return false;

            try
            {
                CancelDownload(url);
                await Task.Delay(200, _cancellationTokenSource.Token);

                item.Progress = 0;
                ResetItemSpeed(item);
                item.BytesReceived = 0;
                item.Status = "Waiting";

                var outputDir = Path.GetDirectoryName(item.OutputPath);
                return await StartDownloadAsync(url, item.FileName, outputDir);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public int GetQueuedDownloadsCount()
        {
            return _downloadQueue.Count;
        }

        public void PauseAllDownloads()
        {
            if (_disposed) return;

            var downloadingItems = _activeDownloads.Values
                .Where(d => d.Status == "Downloading")
                .ToList();

            foreach (var item in downloadingItems)
            {
                PauseDownload(item.Url);
            }
        }

        public void ResumeAllDownloads()
        {
            if (_disposed) return;

            var pausedItems = _activeDownloads.Values
                .Where(d => d.Status == "Paused")
                .ToList();

            foreach (var item in pausedItems)
            {
                ResumeDownload(item.Url);
            }
        }

        public void UpdateChunkCount(int value)
        {
            if (_disposed || value <= 0) return;
            _config.ChunkCount = value;
        }

        public void UpdateMaxConcurrentDownloads(int value)
        {
            if (_disposed || value <= 0 || value == _maxConcurrentDownloads)
                return;

            try
            {
                _maxConcurrentDownloads = value;

                var newSemaphore = new SemaphoreSlim(_maxConcurrentDownloads, _maxConcurrentDownloads);
                var oldSemaphore = Interlocked.Exchange(ref _concurrencyLimitSemaphore, newSemaphore);
                oldSemaphore?.Dispose();
            }
            catch (Exception)
            {
                // 忽略更新错误
            }
        }

        public void UpdateSpeedLimit(long speedLimitBytesPerSecond)
        {
            if (_disposed)
                return;

            ArgumentOutOfRangeException.ThrowIfNegative(speedLimitBytesPerSecond);
            _config.MaximumBytesPerSecond = speedLimitBytesPerSecond;
        }

        public void RemoveCompletedDownloads()
        {
            if (_disposed) return;

            var completedUrls = _activeDownloads
                .Where(kvp => kvp.Value.Status is "Completed" or "Failed" or "Cancelled")
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var url in completedUrls)
            {
                _activeDownloads.TryRemove(url, out _);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _cancellationTokenSource.Cancel();

                var downloaders = _downloaders.Values.ToList();
                foreach (var downloader in downloaders)
                {
                    try
                    {
                        downloader.CancelAsync();
                        downloader.Dispose();
                    }
                    catch (Exception)
                    {
                        // 忽略清理错误
                    }
                }

                _downloaders.Clear();
                _activeDownloads.Clear();

                _cancellationTokenSource?.Dispose();
                _concurrencyLimitSemaphore?.Dispose();
                _queueProcessingSemaphore?.Dispose();
            }
            catch (Exception)
            {
                // 忽略清理错误
            }
        }

        private record DownloadRequest(string Url, string FileName, string OutputPath);
    }
}