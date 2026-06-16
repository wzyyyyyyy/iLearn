using iLearn.Models;
using iLearn.Notifications;
using iLearn.Platform;

namespace iLearn.ViewModels.Pages;

public partial class SettingViewModel : ObservableObject
{
    private readonly AppConfig _appConfig;
    private readonly IPlatformLauncher _launcher;
    private readonly INotificationService _notifications;
    private bool _suppressSave;

    [ObservableProperty]
    private string _appDescription = "学在吉大桌面客户端";

    [ObservableProperty]
    private string _appVersion = GetAppVersion();

    [ObservableProperty]
    private string _lastChecked = "从未检查";

    [ObservableProperty]
    private int _maxConcurrentDownloads;

    [ObservableProperty]
    private int _chunkCount;

    [ObservableProperty]
    private double _speedLimitMBps;

    [ObservableProperty]
    private string _downloadPath = string.Empty;

    public SettingViewModel(
        AppConfig appConfig,
        IPlatformLauncher launcher,
        INotificationService notifications)
    {
        _appConfig = appConfig;
        _launcher = launcher;
        _notifications = notifications;

        _suppressSave = true;
        MaxConcurrentDownloads = _appConfig.MaxConcurrentDownloads;
        ChunkCount = _appConfig.ChunkCount;
        SpeedLimitMBps = _appConfig.SpeedLimitBytesPerSecond / (1024.0 * 1024.0);
        DownloadPath = _appConfig.DownloadPath;
        _suppressSave = false;
    }

    partial void OnMaxConcurrentDownloadsChanged(int value)
    {
        if (_suppressSave || value is < 1 or > 10)
            return;

        _appConfig.MaxConcurrentDownloads = value;
        SaveDownloadSettings("同时下载数已更新");
    }

    partial void OnChunkCountChanged(int value)
    {
        if (_suppressSave || value is < 1 or > 32)
            return;

        _appConfig.ChunkCount = value;
        SaveDownloadSettings("分块数已更新");
    }

    partial void OnSpeedLimitMBpsChanged(double value)
    {
        if (_suppressSave || value < 0)
            return;

        _appConfig.SpeedLimitBytesPerSecond = (long)(value * 1024 * 1024);
        SaveDownloadSettings("下载限速已更新");
    }

    partial void OnDownloadPathChanged(string value)
    {
        if (_suppressSave || string.IsNullOrWhiteSpace(value))
            return;

        _appConfig.DownloadPath = value;
        SaveDownloadSettings("下载目录已更新");
    }

    [RelayCommand]
    private async Task OpenDownloadPath()
    {
        await _launcher.OpenFolderAsync(DownloadPath);
    }

    [RelayCommand]
    private void ResetDownloadSettings()
    {
        _suppressSave = true;
        MaxConcurrentDownloads = 3;
        ChunkCount = 8;
        SpeedLimitMBps = 0;
        DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "iLearnVideo");
        _suppressSave = false;

        _appConfig.MaxConcurrentDownloads = MaxConcurrentDownloads;
        _appConfig.ChunkCount = ChunkCount;
        _appConfig.SpeedLimitBytesPerSecond = 0;
        _appConfig.DownloadPath = DownloadPath;
        _appConfig.Save();
        _notifications.Show("设置已恢复", "下载设置已恢复默认值", AppNotificationKind.Success);
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _notifications.Show("检查更新", "跨平台更新检查将在更新服务接入后启用", AppNotificationKind.Info);
    }

    private void SaveDownloadSettings(string message)
    {
        _appConfig.Save();
        _notifications.Show("设置已保存", message, AppNotificationKind.Success);
    }

    private static string GetAppVersion()
    {
        var version = typeof(SettingViewModel).Assembly.GetName().Version;
        return version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
