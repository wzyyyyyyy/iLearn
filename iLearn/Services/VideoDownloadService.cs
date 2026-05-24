using Downloader;
using iLearn.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly ConcurrentDictionary<string, int> _downloadVersions = new();
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
                MaxTryAgainOnFailure = 10,
                MaximumBytesPerSecond = Math.Max(0, _appConfig.SpeedLimitBytesPerSecond),
                RequestConfiguration = new RequestConfiguration
                {
                    Accept = "*/*",
                    Timeout = 200000
                }
            };

            _concurrencyLimitSemaphore = new SemaphoreSlim(_maxConcurrentDownloads, _maxConcurrentDownloads);
        }

        public async Task<bool> StartDownloadAsync(string url, string fileName, string outputPath, string perspective = "")
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VideoDownloadService));

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(outputPath))
                return false;

            Directory.CreateDirectory(outputPath);
            string fullPath = Path.Combine(outputPath, fileName);

            var item = new DownloadItem
            {
                Url = url,
                FileName = fileName,
                OutputPath = fullPath,
                Status = DownloadStatus.Waiting,
                Speed = "0 KB/s",
                SpeedValue = 0,
                Perspective = perspective ?? string.Empty
            };

            if (!_activeDownloads.TryAdd(url, item))
                return false;

            EnqueueDownload(url, fileName, outputPath, item);

            return true;
        }

        private void EnqueueDownload(string url, string fileName, string outputPath, DownloadItem item)
        {
            var version = _downloadVersions.AddOrUpdate(url, 1, (_, current) => current + 1);
            _downloadQueue.Enqueue(new DownloadRequest(url, fileName, outputPath, item, version));

            _ = Task.Run(() => ProcessDownloadQueueAsync(_cancellationTokenSource.Token));
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
                    if (!IsCurrentRequest(downloadRequest, out var item))
                        continue;

                    lock (item)
                    {
                        if (!IsCurrentRequest(downloadRequest, out _) || item.Status == DownloadStatus.Cancelled)
                            continue;

                        item.Status = DownloadStatus.Queued;
                    }

                    await _concurrencyLimitSemaphore.WaitAsync(cancellationToken);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await StartActualDownloadAsync(downloadRequest, cancellationToken);
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

        private bool IsCurrentRequest(DownloadRequest downloadRequest, out DownloadItem item)
        {
            item = downloadRequest.Item;

            return _activeDownloads.TryGetValue(downloadRequest.Url, out var activeItem)
                && ReferenceEquals(activeItem, downloadRequest.Item)
                && _downloadVersions.TryGetValue(downloadRequest.Url, out var version)
                && version == downloadRequest.Version;
        }

        private async Task StartActualDownloadAsync(DownloadRequest downloadRequest, CancellationToken cancellationToken)
        {
            if (!IsCurrentRequest(downloadRequest, out var item) || item.Status == DownloadStatus.Cancelled)
                return;

            string fullPath = Path.Combine(downloadRequest.OutputPath, downloadRequest.FileName);
            DownloadService? downloader = null;

            try
            {
                lock (item)
                {
                    if (!IsCurrentRequest(downloadRequest, out _) || item.Status == DownloadStatus.Cancelled)
                        return;
                }

                downloader = new DownloadService(_config);
                _downloaders[downloadRequest.Url] = downloader;

                downloader.DownloadProgressChanged += (s, e) => UpdateProgress(downloadRequest, item, e);
                downloader.DownloadFileCompleted += (s, e) => OnDownloadCompleted(downloadRequest, item, e);

                lock (item)
                {
                    if (!IsCurrentRequest(downloadRequest, out _) || item.Status == DownloadStatus.Cancelled)
                        return;

                    item.Status = DownloadStatus.Downloading;
                }
                await downloader.DownloadFileTaskAsync(downloadRequest.Url, fullPath, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (IsCurrentRequest(downloadRequest, out _))
                    item.Status = DownloadStatus.Cancelled;
            }
            catch (Exception ex)
            {
                if (IsCurrentRequest(downloadRequest, out _))
                {
                    item.Status = DownloadStatus.Failed;
                    item.ErrorMessage = ex.Message;
                }
            }
            finally
            {
                if (downloader != null)
                {
                    ((ICollection<KeyValuePair<string, DownloadService>>)_downloaders)
                        .Remove(new KeyValuePair<string, DownloadService>(downloadRequest.Url, downloader));
                    downloader.Dispose();
                }

                if (IsCurrentRequest(downloadRequest, out _))
                    ResetItemSpeed(item);
            }
        }

        private void UpdateProgress(DownloadRequest downloadRequest, DownloadItem item, DownloadProgressChangedEventArgs e)
        {
            if (_disposed || !IsCurrentRequest(downloadRequest, out _) || item.Status == DownloadStatus.Cancelled)
                return;

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

        private void OnDownloadCompleted(DownloadRequest downloadRequest, DownloadItem item, AsyncCompletedEventArgs e)
        {
            if (_disposed || !IsCurrentRequest(downloadRequest, out _))
                return;

            if (item.Status == DownloadStatus.Cancelled)
            {
                ResetItemSpeed(item);
                return;
            }

            if (e.Cancelled)
                item.Status = DownloadStatus.Cancelled;
            else if (e.Error != null)
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = e.Error.Message;
            }
            else
                item.Status = DownloadStatus.Completed;

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
                    item.Status = DownloadStatus.Paused;
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
                    item.Status = DownloadStatus.Downloading;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CancelDownload(string url)
        {
            return CancelDownload(url, deleteFile: true);
        }

        private bool CancelDownload(string url, bool deleteFile)
        {
            if (_disposed)
                return false;

            DownloadItem? item = null;

            try
            {
                var hadDownloader = false;
                var hadItem = _activeDownloads.TryGetValue(url, out item);

                if (_downloaders.TryGetValue(url, out var downloader))
                {
                    hadDownloader = true;
                    downloader.CancelAsync();
                    _downloaders.TryRemove(url, out _);
                }

                if (!hadItem && !hadDownloader)
                    return false;

                _downloadVersions.AddOrUpdate(url, 1, (_, current) => current + 1);

                if (item != null)
                {
                    lock (item)
                    {
                        item.Status = DownloadStatus.Cancelled;
                        ResetItemSpeed(item);
                    }
                }

                if (deleteFile && item != null)
                    _ = DeleteCancelledFileAsync(item);

                return true;
            }
            catch (Exception ex)
            {
                if (item != null)
                    item.ErrorMessage = ex.Message;

                return false;
            }
        }

        private static async Task DeleteCancelledFileAsync(DownloadItem item)
        {
            try
            {
                await Task.Delay(1000);

                if (File.Exists(item.OutputPath))
                    File.Delete(item.OutputPath);
            }
            catch (Exception ex)
            {
                item.ErrorMessage = ex.Message;
            }
        }

        public async Task<bool> RetryDownloadAsync(string url)
        {
            if (_disposed || !_activeDownloads.TryGetValue(url, out var item))
                return false;

            try
            {
                if (!CancelDownload(url, deleteFile: false))
                    return false;

                await Task.Delay(200, _cancellationTokenSource.Token);

                item.Progress = 0;
                ResetItemSpeed(item);
                item.BytesReceived = 0;
                item.Status = DownloadStatus.Waiting;

                var outputDir = Path.GetDirectoryName(item.OutputPath);
                if (string.IsNullOrWhiteSpace(outputDir))
                    return false;

                EnqueueDownload(url, item.FileName, outputDir, item);
                return true;
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
                .Where(d => d.Status == DownloadStatus.Downloading)
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
                .Where(d => d.Status == DownloadStatus.Paused)
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
                .Where(kvp => kvp.Value.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
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
                _downloadVersions.Clear();

                _cancellationTokenSource?.Dispose();
                _concurrencyLimitSemaphore?.Dispose();
                _queueProcessingSemaphore?.Dispose();
            }
            catch (Exception)
            {
                // 忽略清理错误
            }
        }

        private record DownloadRequest(string Url, string FileName, string OutputPath, DownloadItem Item, int Version);
    }
}
