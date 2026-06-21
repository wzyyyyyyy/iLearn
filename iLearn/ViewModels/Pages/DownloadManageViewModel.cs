using iLearn.Downloads;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Platform;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace iLearn.ViewModels.Pages;

public partial class DownloadManageViewModel : ObservableObject
{
    private readonly DownloadQueueService _downloadQueue;
    private readonly INotificationService _notifications;
    private readonly IPlatformLauncher _launcher;
    private readonly AppConfig _appConfig;
    private int _lastFailedDownloadsCount;

    public DownloadManageViewModel(
        DownloadQueueService downloadQueue,
        INotificationService notifications,
        IPlatformLauncher launcher,
        AppConfig appConfig)
    {
        _downloadQueue = downloadQueue;
        _notifications = notifications;
        _launcher = launcher;
        _appConfig = appConfig;
        Downloads = _downloadQueue.Tasks;

        if (Downloads is INotifyCollectionChanged collectionChanged)
            collectionChanged.CollectionChanged += (_, _) => RaiseSummaryChanged();
    }

    public ReadOnlyObservableCollection<DownloadTaskSnapshot> Downloads { get; }

    public int ActiveDownloadsCount => Downloads.Count(download => download.Status == DownloadTaskStatus.Downloading);

    public int CompletedDownloadsCount => Downloads.Count(download => download.Status == DownloadTaskStatus.Completed);

    public int QueuedDownloadsCount => Downloads.Count(download => download.Status is DownloadTaskStatus.Queued or DownloadTaskStatus.Waiting);

    public int FailedDownloadsCount => Downloads.Count(download => download.Status == DownloadTaskStatus.Failed);

    public bool HasDownloads => Downloads.Count > 0;

    public bool HasNoDownloads => !HasDownloads;

    public bool HasDownloadingItems => Downloads.Any(download => download.Status == DownloadTaskStatus.Downloading);

    public bool HasPausedItems => Downloads.Any(download => download.Status == DownloadTaskStatus.Paused);

    public string TotalDownloadSpeed => FormatBytesPerSecond(
        Downloads.Where(download => download.Status == DownloadTaskStatus.Downloading).Sum(download => download.BytesPerSecond));

    [RelayCommand]
    private async Task CancelAllDownload()
    {
        _downloadQueue.ClearCompleted();
        await _downloadQueue.CancelAllAsync();
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        _downloadQueue.ClearCompleted();
        _notifications.Show("已清理", "已移除完成的下载记录", AppNotificationKind.Success);
    }

    [RelayCommand]
    private async Task RetryFailed()
    {
        await _downloadQueue.RetryFailedAsync();
        _notifications.Show("正在重试", "失败任务已重新加入队列", AppNotificationKind.Info);
    }

    [RelayCommand]
    private async Task PauseDownload(DownloadTaskSnapshot item)
    {
        await _downloadQueue.PauseAsync(item.Id);
        _notifications.Show("下载已暂停", item.FileName, AppNotificationKind.Info);
    }

    [RelayCommand]
    private async Task RetryDownload(DownloadTaskSnapshot item)
    {
        try
        {
            await _downloadQueue.RetryAsync(item.Id);
            _notifications.Show("重试开始", item.FileName, AppNotificationKind.Info);
        }
        catch (Exception ex)
        {
            _notifications.Show("重试失败", ex.Message, AppNotificationKind.Error);
        }
    }

    [RelayCommand]
    private async Task CancelDownload(DownloadTaskSnapshot item)
    {
        await _downloadQueue.CancelAsync(item.Id);
        _notifications.Show("下载已取消", item.FileName, AppNotificationKind.Warning);
    }

    [RelayCommand]
    private async Task OpenDownloadFile(DownloadTaskSnapshot item)
    {
        await _launcher.OpenFileAsync(item.OutputPath);
    }

    [RelayCommand]
    private async Task OpenDownloadsFolder()
    {
        await _launcher.OpenFolderAsync(_appConfig.DownloadPath);
    }

    private void RaiseSummaryChanged()
    {
        var failedCount = FailedDownloadsCount;
        if (failedCount > _lastFailedDownloadsCount)
            _notifications.Show("下载失败", "有任务下载失败，可在下载管理中重试", AppNotificationKind.Error);
        _lastFailedDownloadsCount = failedCount;

        OnPropertyChanged(nameof(ActiveDownloadsCount));
        OnPropertyChanged(nameof(CompletedDownloadsCount));
        OnPropertyChanged(nameof(QueuedDownloadsCount));
        OnPropertyChanged(nameof(FailedDownloadsCount));
        OnPropertyChanged(nameof(HasDownloads));
        OnPropertyChanged(nameof(HasNoDownloads));
        OnPropertyChanged(nameof(HasDownloadingItems));
        OnPropertyChanged(nameof(HasPausedItems));
        OnPropertyChanged(nameof(TotalDownloadSpeed));
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{bytesPerSecond / 1024 / 1024:0.##} MB/s";
        if (bytesPerSecond >= 1024)
            return $"{bytesPerSecond / 1024:0.##} KB/s";
        return $"{bytesPerSecond:0} B/s";
    }
}
