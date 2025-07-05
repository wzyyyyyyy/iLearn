using Downloader;
using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Pages
{
    public partial class DownloadManageViewModel : ObservableObject
    {
        private readonly VideoDownloadService _downloadService;
        private readonly ISnackbarService _snackbarService;
        private readonly DispatcherTimer _refreshTimer;

        [ObservableProperty]
        private ObservableCollection<DownloadItem> _downloads;

        [ObservableProperty]
        private int _activeDownloadsCount;

        [ObservableProperty]
        private int _completedDownloadsCount;

        [ObservableProperty]
        private int _queuedDownloadsCount;

        [ObservableProperty]
        private string _totalDownloadSpeed = "0 MB/s";

        [ObservableProperty]
        private bool _hasDownloadingItems;

        [ObservableProperty]
        private bool _hasPausedItems;

        public DownloadManageViewModel(
            VideoDownloadService downloadService,
            ISnackbarService snackbarService)
        {
            _downloadService = downloadService;
            _snackbarService = snackbarService;
            Downloads = new ObservableCollection<DownloadItem>();

            // 设置定时器定期刷新下载状态
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += RefreshDownloads;
            _refreshTimer.Start();

            RefreshDownloads(null, null);
        }

        private void RefreshDownloads(object sender, EventArgs e)
        {
            var activeDownloads = _downloadService.ActiveDownloads.ToList();

            // 更新或添加新的下载项
            foreach (var download in activeDownloads)
            {
                var existingItem = Downloads.FirstOrDefault(d => d.Url == download.Url);
                if (existingItem == null)
                {
                    Downloads.Add(download);
                }
            }

            // 更新统计信息
            ActiveDownloadsCount = Downloads.Count(d => d.Status == "Downloading");
            CompletedDownloadsCount = Downloads.Count(d => d.Status == "Completed");
            QueuedDownloadsCount = Downloads.Count(d => d.Status == "Queued") + _downloadService.GetQueuedDownloadsCount();

            // 更新按钮状态
            HasDownloadingItems = Downloads.Any(d => d.Status == "Downloading");
            HasPausedItems = Downloads.Any(d => d.Status == "Paused");

            // 计算总下载速度，转换为MB/s
            var totalSpeedBytes = Downloads
                .Where(d => d.Status == "Downloading")
                .Sum(d => d.SpeedValue);

            var totalSpeedMB = totalSpeedBytes / (1024.0 * 1024.0);
            TotalDownloadSpeed = $"{totalSpeedMB:F2} MB/s";
        }

        private double ParseSpeed(string speedText)
        {
            if (string.IsNullOrEmpty(speedText))
                return 0;

            // 处理KB/s格式
            if (speedText.Contains("KB/s"))
            {
                var parts = speedText.Split(' ');
                if (parts.Length > 0 && double.TryParse(parts[0], out var speed))
                    return speed;
            }
            // 处理MB/s格式
            else if (speedText.Contains("MB/s"))
            {
                var parts = speedText.Split(' ');
                if (parts.Length > 0 && double.TryParse(parts[0], out var speed))
                    return speed * 1024; // 转换为KB/s用于统计
            }

            return 0;
        }

        [RelayCommand]
        private void PauseAllDownloads()
        {
            _downloadService.PauseAllDownloads();
            ShowSnackbar("全部暂停", "已暂停所有正在下载的任务", ControlAppearance.Info);
        }

        [RelayCommand]
        private async Task ResumeAllDownloads()
        {
            await _downloadService.ResumeAllDownloadsAsync();
            ShowSnackbar("全部开始", "已恢复所有暂停的下载任务", ControlAppearance.Success);
        }

        [RelayCommand]
        private void PauseDownload(DownloadItem item)
        {
            if (item?.Status == "Downloading")
            {
                var success = _downloadService.PauseDownload(item.Url);
                if (success)
                {
                    ShowSnackbar("下载已暂停", $"已暂停下载: {item.FileName}", ControlAppearance.Info);
                }
            }
        }

        [RelayCommand]
        private void ResumeDownload(DownloadItem item)
        {
            if (item?.Status == "Paused")
            {
                var success = _downloadService.ResumeDownload(item.Url);
                if (success)
                {
                    ShowSnackbar("下载已恢复", $"已恢复下载: {item.FileName}", ControlAppearance.Success);
                }
            }
        }

        [RelayCommand]
        private void CancelDownload(DownloadItem item)
        {
            if (item != null && item.Status != "Completed")
            {
                var success = _downloadService.CancelDownload(item.Url);
                if (success)
                {
                    ShowSnackbar("下载已取消", $"已取消下载: {item.FileName}", ControlAppearance.Caution);
                }
            }
        }

        [RelayCommand]
        private async Task RetryDownload(DownloadItem item)
        {
            if (item?.Status == "Failed")
            {
                try
                {
                    await _downloadService.RetryDownloadAsync(item.Url);
                    ShowSnackbar("重试开始", $"正在重试下载: {item.FileName}", ControlAppearance.Info);
                }
                catch (Exception ex)
                {
                    ShowSnackbar("重试失败", $"无法重试下载: {ex.Message}", ControlAppearance.Danger);
                }
            }
        }

        [RelayCommand]
        private void OpenDownloadFile(DownloadItem item)
        {
            try
            {
                if (item != null && item.Status == "Completed" && File.Exists(item.OutputPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.OutputPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    ShowSnackbar("文件不存在", "下载文件不存在或下载未完成", ControlAppearance.Caution);
                }
            }
            catch (Exception ex)
            {
                ShowSnackbar("打开失败", $"无法打开文件: {ex.Message}", ControlAppearance.Danger);
            }
        }

        [RelayCommand]
        private void OpenDownloadsFolder()
        {
            try
            {
                var folder = Path.Combine(Environment.CurrentDirectory, "Downloads");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                ShowSnackbar("打开失败", $"无法打开下载文件夹: {ex.Message}", ControlAppearance.Danger);
            }
        }

        [RelayCommand]
        private void RemoveDownload(DownloadItem item)
        {
            if (item != null)
            {
                if (item.Status == "Downloading")
                {
                    _downloadService.CancelDownload(item.Url);
                }
                Downloads.Remove(item);
                ShowSnackbar("已移除", $"已从列表中移除: {item.FileName}", ControlAppearance.Info);
            }
        }

        private void ShowSnackbar(string title, string message, ControlAppearance appearance)
        {
            var icon = appearance switch
            {
                ControlAppearance.Success => new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                ControlAppearance.Danger => new SymbolIcon(SymbolRegular.ErrorCircle24),
                ControlAppearance.Info => new SymbolIcon(SymbolRegular.Info24),
                ControlAppearance.Caution => new SymbolIcon(SymbolRegular.Warning24),
                _ => new SymbolIcon(SymbolRegular.Info24)
            };

            _snackbarService.Show(
                title,
                message,
                appearance,
                icon,
                TimeSpan.FromSeconds(4)
            );
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
        }
    }
}