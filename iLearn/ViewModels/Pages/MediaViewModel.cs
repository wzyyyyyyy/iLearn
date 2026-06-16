using Avalonia.Platform;
using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.Platform;
using iLearn.Services;
using iLearn.ViewModels;
using System.Collections.ObjectModel;

namespace iLearn.ViewModels.Pages;

public sealed partial class MediaViewModel : AppViewModelBase
{
    private readonly ILearnApiService _ilearnApiService;
    private readonly INotificationService _notifications;
    private readonly IPlatformLauncher _launcher;
    private readonly List<LiveAndRecordInfo> _sharedLiveAndRecordInfos;
    private List<LiveAndRecordInfo> _allMediaItems = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LiveAndRecordInfo> _mediaItems = [];

    [ObservableProperty]
    private ClassInfo? _currentCourse;

    public MediaViewModel(
        ILearnApiService ilearnApiService,
        NavigationService navigationService,
        INotificationService notifications,
        IPlatformLauncher launcher,
        List<LiveAndRecordInfo> liveAndRecordInfos)
    {
        _ilearnApiService = ilearnApiService;
        _notifications = notifications;
        _launcher = launcher;
        _sharedLiveAndRecordInfos = liveAndRecordInfos;

        WeakReferenceMessenger.Default.Register<CourseMessage>(this, async (_, message) =>
        {
            CurrentCourse = message.classInfo;
            await LoadDataAsync();
        });
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplySearch();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task OpenMediaAsync(LiveAndRecordInfo? media)
    {
        if (media is null || string.IsNullOrWhiteSpace(media.ResourceId))
        {
            _notifications.Show("无法播放", "该资源没有视频可供播放", AppNotificationKind.Warning);
            return;
        }

        BeginBusy("正在获取视频地址和字幕...");
        _notifications.Show("正在加载视频", "正在获取视频地址和字幕", AppNotificationKind.Info);

        try
        {
            var videoInfo = await _ilearnApiService.GetVideoInfoAsync(media.ResourceId);
            var content = await ReadPlayerTemplateAsync();
            content = content
                .Replace("_LEFTVIDEO_", videoInfo.VideoList.ElementAtOrDefault(0)?.VideoPath ?? string.Empty)
                .Replace("_RIGHTVIDEO_", videoInfo.VideoList.ElementAtOrDefault(1)?.VideoPath ?? string.Empty)
                .Replace("_SUBTITLE_", videoInfo.PhaseUrl ?? string.Empty)
                .Replace("_SUBTITLE_DATA_", string.Empty);

            var tempFile = Path.Combine(Path.GetTempPath(), $"video_preview_{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(tempFile, content);
            await _launcher.OpenFileAsync(tempFile);
            _notifications.Show("播放器已打开", media.LiveRecordName, AppNotificationKind.Success);
        }
        catch (Exception ex)
        {
            _notifications.Show("播放失败", ex.Message, AppNotificationKind.Error);
            StatusText = "播放失败";
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task LoadDataAsync()
    {
        if (CurrentCourse is null)
        {
            MediaItems.Clear();
            StatusText = "请先选择课程";
            return;
        }

        BeginBusy("正在加载课程视频...");
        try
        {
            var items = await _ilearnApiService.GetLiveAndRecordInfoAsync(CurrentCourse.TermId ?? string.Empty, CurrentCourse.ClassId);
            _allMediaItems = items;
            _sharedLiveAndRecordInfos.Clear();
            _sharedLiveAndRecordInfos.AddRange(items);
            ApplySearch();
            StatusText = items.Count == 0 ? "该课程暂无视频" : $"已加载 {items.Count} 个视频";
        }
        catch (Exception ex)
        {
            _notifications.Show("视频加载失败", ex.Message, AppNotificationKind.Error);
            StatusText = "视频加载失败";
        }
        finally
        {
            EndBusy();
        }
    }

    private void ApplySearch()
    {
        var query = SearchQuery.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allMediaItems
            : _allMediaItems.Where(item =>
                item.LiveRecordName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.TeacherName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        MediaItems = new ObservableCollection<LiveAndRecordInfo>(filtered);
    }

    private static async Task<string> ReadPlayerTemplateAsync()
    {
        await using var stream = AssetLoader.Open(new Uri("avares://iLearn/Assets/VideoPlayer.html"));
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
