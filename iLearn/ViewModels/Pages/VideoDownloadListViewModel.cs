using iLearn.Downloads;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace iLearn.ViewModels.Pages;

public partial class VideoDownloadListViewModel : ObservableObject
{
    private readonly DownloadQueueService _downloadQueue;
    private readonly ILearnApiService _iLearnApiService;
    private readonly INotificationService _notifications;
    private readonly AppConfig _appConfig;
    private bool _isUpdatingAllSelected;

    [ObservableProperty]
    private ObservableCollection<LiveAndRecordInfo> _videos;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isAllHdmiSelected;

    [ObservableProperty]
    private bool _isAllTeacherSelected;

    [ObservableProperty]
    private int _selectedDownloadCount;

    [ObservableProperty]
    private bool _isPreparingDownloads;

    public VideoDownloadListViewModel(
        List<LiveAndRecordInfo> liveAndRecordInfos,
        DownloadQueueService downloadQueue,
        ILearnApiService iLearnApiService,
        INotificationService notifications,
        AppConfig appConfig)
    {
        _downloadQueue = downloadQueue;
        _iLearnApiService = iLearnApiService;
        _notifications = notifications;
        _appConfig = appConfig;

        Videos = new ObservableCollection<LiveAndRecordInfo>(liveAndRecordInfos ?? []);

        foreach (var video in Videos)
            video.PropertyChanged += OnVideoPropertyChanged;

        RefreshSelectedDownloadCount();
    }

    public string SelectedDownloadText => SelectedDownloadCount == 0
        ? "未选择"
        : $"已选择 {SelectedDownloadCount} 个文件";

    public string DownloadButtonText => IsPreparingDownloads ? "正在准备..." : "加入下载队列";

    public ObservableCollection<LiveAndRecordInfo> FilteredVideos =>
        string.IsNullOrWhiteSpace(SearchText)
            ? Videos
            : new ObservableCollection<LiveAndRecordInfo>(
                Videos.Where(video =>
                    Contains(video.LiveRecordName, SearchText) ||
                    Contains(video.TeacherName, SearchText)));

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredVideos));
    }

    partial void OnIsPreparingDownloadsChanged(bool value)
    {
        OnPropertyChanged(nameof(DownloadButtonText));
    }

    partial void OnIsAllHdmiSelectedChanged(bool value)
    {
        SetAllSelections(nameof(LiveAndRecordInfo.IsHdmiSelected), value);
    }

    partial void OnIsAllTeacherSelectedChanged(bool value)
    {
        SetAllSelections(nameof(LiveAndRecordInfo.IsTeacherSelected), value);
    }

    partial void OnSelectedDownloadCountChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedDownloadText));
    }

    [RelayCommand]
    private async Task DownloadSelected()
    {
        if (IsPreparingDownloads)
            return;

        var selections = Videos
            .Where(video => !string.IsNullOrWhiteSpace(video.ResourceId))
            .SelectMany(video => new[]
            {
                new VideoSelection(video, "HDMI", video.IsHdmiSelected),
                new VideoSelection(video, "教师", video.IsTeacherSelected)
            })
            .Where(selection => selection.Selected)
            .ToList();

        if (selections.Count == 0)
        {
            _notifications.Show("请选择要下载的视频", "没有选中任何视频进行下载", AppNotificationKind.Info);
            return;
        }

        IsPreparingDownloads = true;
        var queued = 0;
        var failed = 0;
        var subtitleResourceIds = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            foreach (var selection in selections)
            {
                var shouldQueueSubtitle = subtitleResourceIds.Add(selection.Video.ResourceId);
                if (await DownloadVideoAsync(selection.Video, selection.Perspective, shouldQueueSubtitle))
                {
                    queued++;
                    SetSelection(selection.Video, selection.Perspective, false);
                }
                else
                {
                    failed++;
                }
            }
        }
        finally
        {
            IsPreparingDownloads = false;
        }

        RefreshSelectedDownloadCount();
        _notifications.Show(
            "下载任务已添加",
            $"成功加入队列 {queued} 个，失败 {failed} 个",
            failed == 0 ? AppNotificationKind.Success : AppNotificationKind.Warning);
    }

    private async Task<bool> DownloadVideoAsync(LiveAndRecordInfo video, string perspective, bool shouldQueueSubtitle)
    {
        try
        {
            Directory.CreateDirectory(_appConfig.DownloadPath);

            var videoInfo = await _iLearnApiService.GetVideoInfoAsync(video.ResourceId);
            if (shouldQueueSubtitle)
                await DownloadSubtitleAsync(videoInfo, video.ResourceId);

            var videoSource = perspective == "HDMI"
                ? videoInfo.VideoList.ElementAtOrDefault(1)
                : videoInfo.VideoList.ElementAtOrDefault(0);

            if (string.IsNullOrWhiteSpace(videoSource?.VideoPath))
                return false;

            var fileName = FileNameService.BuildVideoFileName(video.LiveRecordName, perspective);
            await _downloadQueue.EnqueueAsync(new DownloadRequest(
                Id: $"{video.ResourceId}-{perspective}",
                Url: videoSource.VideoPath,
                FileName: fileName,
                OutputDirectory: _appConfig.DownloadPath,
                DisplayName: video.LiveRecordName,
                Perspective: perspective));
            return true;
        }
        catch (Exception ex)
        {
            _notifications.Show(
                "下载失败",
                $"无法下载视频 {video.LiveRecordName}: {ex.Message}",
                AppNotificationKind.Error);
            return false;
        }
    }

    private async Task DownloadSubtitleAsync(VideoInfo videoInfo, string fallbackId)
    {
        if (string.IsNullOrWhiteSpace(videoInfo.PhaseUrl))
            return;

        var fileName = FileNameService.BuildSubtitleFileName(videoInfo.ResourceName);
        var folder = Path.Combine(_appConfig.DownloadPath, "Subtitles");

        await _downloadQueue.EnqueueAsync(new DownloadRequest(
            Id: $"{(string.IsNullOrWhiteSpace(videoInfo.LiveRecordId) ? fallbackId : videoInfo.LiveRecordId)}-subtitle",
            Url: videoInfo.PhaseUrl,
            FileName: fileName,
            OutputDirectory: folder,
            DisplayName: $"{videoInfo.ResourceName} 字幕",
            Perspective: "字幕"));
    }

    private void SetAllSelections(string propertyName, bool selected)
    {
        if (_isUpdatingAllSelected)
            return;

        _isUpdatingAllSelected = true;
        try
        {
            foreach (var video in Videos)
            {
                if (propertyName == nameof(LiveAndRecordInfo.IsHdmiSelected))
                    video.IsHdmiSelected = selected;
                else
                    video.IsTeacherSelected = selected;
            }
        }
        finally
        {
            _isUpdatingAllSelected = false;
        }

        RefreshSelectedDownloadCount();
    }

    private void OnVideoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(LiveAndRecordInfo.IsHdmiSelected) or nameof(LiveAndRecordInfo.IsTeacherSelected)))
            return;

        if (!_isUpdatingAllSelected)
        {
            _isUpdatingAllSelected = true;
            try
            {
                IsAllHdmiSelected = Videos.Count > 0 && Videos.All(video => video.IsHdmiSelected);
                IsAllTeacherSelected = Videos.Count > 0 && Videos.All(video => video.IsTeacherSelected);
            }
            finally
            {
                _isUpdatingAllSelected = false;
            }
        }

        RefreshSelectedDownloadCount();
    }

    private void SetSelection(LiveAndRecordInfo video, string perspective, bool selected)
    {
        if (perspective == "HDMI")
            video.IsHdmiSelected = selected;
        else if (perspective == "教师")
            video.IsTeacherSelected = selected;
    }

    private void RefreshSelectedDownloadCount()
    {
        SelectedDownloadCount = Videos.Count(video => video.IsHdmiSelected) + Videos.Count(video => video.IsTeacherSelected);
    }

    private static bool Contains(string? value, string searchText)
    {
        return value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
    }

    private sealed record VideoSelection(LiveAndRecordInfo Video, string Perspective, bool Selected);
}
