using Downloader;
using iLearn.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace iLearn.Services
{
    public class VideoDownloadService
    {
        public IEnumerable<DownloadItem> ActiveDownloads => _activeDownloads.Values;

        private readonly ConcurrentDictionary<string, DownloadItem> _activeDownloads = new();
        private readonly ConcurrentDictionary<string, DownloadService> _downloaders = new();
        private readonly Queue<(string url, string fileName, string outputPath)> _downloadQueue = new();
        private readonly object _queueLock = new object();
        private const int MaxConcurrentDownloads = 3;

        private readonly DownloadConfiguration _config = new()
        {
            ChunkCount = 8,
            ParallelDownload = true,
            Timeout = 5000,
            BufferBlockSize = 10240,
            MaxTryAgainOnFailover = 3,
            RequestConfiguration = {
                Accept = "*/*",
                Timeout = 20000
            }
        };

        public async Task StartDownloadAsync(string url, string fileName, string outputPath)
        {
            if (_activeDownloads.ContainsKey(url))
                return;

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

            var currentDownloading = _activeDownloads.Values.Count(d => d.Status == "Downloading");
            if (currentDownloading >= MaxConcurrentDownloads)
            {
                lock (_queueLock)
                {
                    _downloadQueue.Enqueue((url, fileName, outputPath));
                }
                item.Status = "Queued";
                return;
            }

            await StartActualDownloadAsync(url, fileName, outputPath);
        }

        private async Task StartActualDownloadAsync(string url, string fileName, string outputPath)
        {
            string fullPath = Path.Combine(outputPath, fileName);

            if (!_activeDownloads.TryGetValue(url, out var item))
                return;

            var downloader = new DownloadService(_config);

            downloader.DownloadProgressChanged += (s, e) =>
            {
                item.Progress = e.ProgressPercentage;
                var speedKB = e.BytesPerSecondSpeed / 1024.0;
                item.Speed = speedKB >= 1024 ? $"{speedKB / 1024.0:F2} MB/s" : $"{speedKB:F2} KB/s";
                item.SpeedValue = e.BytesPerSecondSpeed;
                item.BytesReceived = e.ReceivedBytesSize;
                item.TotalBytes = e.TotalBytesToReceive;
                item.Status = "Downloading";
            };

            downloader.DownloadFileCompleted += async (s, e) =>
            {
                if (e.Cancelled)
                    item.Status = "Cancelled";
                else if (e.Error != null)
                    item.Status = "Failed";
                else
                    item.Status = "Completed";

                item.Speed = "0 KB/s";
                item.SpeedValue = 0;
                _downloaders.TryRemove(url, out _);

                await ProcessDownloadQueue();
            };

            _downloaders[url] = downloader;

            try
            {
                await downloader.DownloadFileTaskAsync(url, fullPath);
            }
            catch
            {
                item.Status = "Failed";
                item.Speed = "0 KB/s";
                item.SpeedValue = 0;
                _downloaders.TryRemove(url, out _);

                await ProcessDownloadQueue();
            }
        }

        private async Task ProcessDownloadQueue()
        {
            lock (_queueLock)
            {
                var currentDownloading = _activeDownloads.Values.Count(d => d.Status == "Downloading");
                if (currentDownloading >= MaxConcurrentDownloads || _downloadQueue.Count == 0)
                    return;

                var (url, fileName, outputPath) = _downloadQueue.Dequeue();

                Task.Run(async () =>
                {
                    await StartActualDownloadAsync(url, fileName, outputPath);
                });
            }
        }

        public bool PauseDownload(string url)
        {
            if (_downloaders.TryGetValue(url, out var d))
            {
                d.Pause();
                if (_activeDownloads.TryGetValue(url, out var item))
                {
                    item.Status = "Paused";
                    item.Speed = "0 KB/s";
                    item.SpeedValue = 0;
                }
                return true;
            }
            return false;
        }

        public bool ResumeDownload(string url)
        {
            if (_downloaders.TryGetValue(url, out var d))
            {
                d.Resume();
                if (_activeDownloads.TryGetValue(url, out var item))
                    item.Status = "Downloading";
                return true;
            }
            return false;
        }

        public bool CancelDownload(string url)
        {
            if (_downloaders.TryGetValue(url, out var d))
            {
                d.CancelAsync();
                if (_activeDownloads.TryGetValue(url, out var item))
                {
                    item.Status = "Cancelled";
                    item.Speed = "0 KB/s";
                    item.SpeedValue = 0;
                }

                _downloaders.TryRemove(url, out _);

                Task.Run(async () =>
                {
                    await ProcessDownloadQueue();
                });

                return true;
            }
            return false;
        }

        public async Task RetryDownloadAsync(string url)
        {
            if (_activeDownloads.TryGetValue(url, out var item))
            {
                item.Progress = 0;
                item.Speed = "0 KB/s";
                item.SpeedValue = 0;
                item.BytesReceived = 0;
                item.Status = "Waiting";

                await StartActualDownloadAsync(url, item.FileName, Path.GetDirectoryName(item.OutputPath));
            }
        }

        public int GetQueuedDownloadsCount()
        {
            lock (_queueLock)
            {
                return _downloadQueue.Count;
            }
        }

        public void PauseAllDownloads()
        {
            var downloadingItems = _activeDownloads.Values.Where(d => d.Status == "Downloading").ToList();
            foreach (var item in downloadingItems)
            {
                PauseDownload(item.Url);
            }
        }

        public async Task ResumeAllDownloadsAsync()
        {
            var pausedItems = _activeDownloads.Values.Where(d => d.Status == "Paused").ToList();
            foreach (var item in pausedItems)
            {
                // 检查并发限制
                var currentDownloading = _activeDownloads.Values.Count(d => d.Status == "Downloading");
                if (currentDownloading >= MaxConcurrentDownloads)
                {
                    lock (_queueLock)
                    {
                        _downloadQueue.Enqueue((item.Url, item.FileName, Path.GetDirectoryName(item.OutputPath)));
                    }
                    item.Status = "Queued";
                }
                else
                {
                    ResumeDownload(item.Url);
                }
            }
        }
    }
}