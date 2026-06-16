using Avalonia.Platform;
using Avalonia.Threading;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Platform;
using iLearn.ViewModels;
using System.Collections.ObjectModel;
using System.Text;

namespace iLearn.ViewModels.Pages;

public sealed partial class LocalVideoViewModel : AppViewModelBase
{
    private readonly INotificationService _notifications;
    private readonly IPlatformLauncher _launcher;
    private readonly AppConfig _appConfig;

    [ObservableProperty]
    private ObservableCollection<LocalVideoFile> _localVideos = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "全部";

    public ObservableCollection<string> FilterOptions { get; } =
    [
        "全部",
        "HDMI视角",
        "教师视角"
    ];

    public ObservableCollection<LocalVideoFile> FilteredVideos
    {
        get
        {
            var filtered = LocalVideos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.Where(video =>
                    video.CourseName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    video.FileName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            filtered = SelectedFilter switch
            {
                "HDMI视角" => filtered.Where(video => video.Perspective.Contains("HDMI", StringComparison.OrdinalIgnoreCase)),
                "教师视角" => filtered.Where(video =>
                    video.Perspective.Contains("教师", StringComparison.OrdinalIgnoreCase) ||
                    video.Perspective.Contains("teacher", StringComparison.OrdinalIgnoreCase)),
                _ => filtered
            };

            return new ObservableCollection<LocalVideoFile>(
                filtered.OrderByDescending(video => video.RecordDate)
                    .ThenBy(video => video.CourseName));
        }
    }

    public LocalVideoViewModel(
        INotificationService notifications,
        IPlatformLauncher launcher,
        AppConfig appConfig)
    {
        _notifications = notifications;
        _launcher = launcher;
        _appConfig = appConfig;
        _ = LoadLocalVideosAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredVideos));
    }

    partial void OnSelectedFilterChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredVideos));
    }

    [RelayCommand]
    private async Task LoadLocalVideosAsync()
    {
        BeginBusy("正在扫描下载目录...");
        try
        {
            var videos = await Task.Run(() =>
            {
                var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".mp4",
                    ".avi",
                    ".mkv",
                    ".mov",
                    ".wmv",
                    ".flv"
                };

                if (!Directory.Exists(_appConfig.DownloadPath))
                    return new List<LocalVideoFile>();

                return Directory
                    .EnumerateFiles(_appConfig.DownloadPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => videoExtensions.Contains(Path.GetExtension(file)))
                    .Select(LocalVideoFile.FromFileName)
                    .ToList();
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LocalVideos = new ObservableCollection<LocalVideoFile>(videos);
                OnPropertyChanged(nameof(FilteredVideos));
            });

            _notifications.Show("本地视频已刷新", $"找到 {videos.Count} 个视频文件", AppNotificationKind.Success);
            StatusText = videos.Count == 0 ? "下载目录中还没有本地视频" : $"找到 {videos.Count} 个本地视频";
        }
        catch (Exception ex)
        {
            _notifications.Show("加载失败", $"无法加载本地视频: {ex.Message}", AppNotificationKind.Error);
            StatusText = "加载失败";
        }
        finally
        {
            EndBusy();
        }
    }

    [RelayCommand]
    private async Task OpenVideoAsync(LocalVideoFile? video)
    {
        if (video is null || !File.Exists(video.FullPath))
        {
            ShowNotification("文件不存在", "视频文件已被移动或删除", AppNotificationKind.Error);
            return;
        }

        try
        {
            var subtitlePath = video.FindSubtitlePath(_appConfig.DownloadPath);
            await OpenLocalVideoAsync(video, subtitlePath);
            ShowNotification(
                "正在打开",
                subtitlePath is null ? $"正在打开 {video.FileName}，未找到匹配字幕" : $"正在打开 {video.FileName}，已自动匹配字幕",
                AppNotificationKind.Info);
        }
        catch (Exception ex)
        {
            ShowNotification("打开失败", $"无法打开视频文件: {ex.Message}", AppNotificationKind.Error);
        }
    }

    [RelayCommand]
    private async Task OpenFileLocationAsync(LocalVideoFile? video)
    {
        if (video is null || !File.Exists(video.FullPath))
        {
            ShowNotification("文件不存在", "视频文件已被移动或删除", AppNotificationKind.Error);
            return;
        }

        var directory = Path.GetDirectoryName(video.FullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            await _launcher.OpenFolderAsync(directory);
    }

    [RelayCommand]
    private void Search()
    {
        OnPropertyChanged(nameof(FilteredVideos));
    }

    private void ShowNotification(string title, string message, AppNotificationKind kind)
    {
        _notifications.Show(title, message, kind);
    }

    private async Task OpenLocalVideoAsync(LocalVideoFile video, string? subtitlePath)
    {
        var content = await ReadPlayerTemplateAsync();
        var partnerVideo = video.GetPartnerVideo();

        content = content
            .Replace("_LEFTVIDEO_", new Uri(video.FullPath).AbsoluteUri)
            .Replace("_RIGHTVIDEO_", partnerVideo is null ? new Uri(video.FullPath).AbsoluteUri : new Uri(partnerVideo.FullPath).AbsoluteUri)
            .Replace("_SUBTITLE_", subtitlePath is null ? string.Empty : new Uri(subtitlePath).AbsoluteUri)
            .Replace("_SUBTITLE_DATA_", subtitlePath is null ? string.Empty : await CreateSubtitleDataAsync(subtitlePath));

        var tempFile = Path.Combine(Path.GetTempPath(), $"video_local_{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(tempFile, content);
        await _launcher.OpenFileAsync(tempFile);
    }

    private static async Task<string> ReadPlayerTemplateAsync()
    {
        await using var stream = AssetLoader.Open(new Uri("avares://iLearn/Assets/VideoPlayer.html"));
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> CreateSubtitleDataAsync(string subtitlePath)
    {
        var subtitleText = await File.ReadAllTextAsync(subtitlePath);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(subtitleText));
    }
}
