using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Pages
{
    public partial class VideoDownloadListViewModel : ObservableObject
    {
        private readonly VideoDownloadService _downloadService;
        private readonly ILearnApiService _iLearnApiService;
        private readonly ISnackbarService _snackbarService;
        private readonly AppConfig _appConfig;
        private bool _isUpdatingAllSelected = false; // 添加标志位防止循环触发

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

        public string SelectedDownloadText => SelectedDownloadCount == 0
            ? "未选择"
            : $"已选择 {SelectedDownloadCount} 个文件";

        public ObservableCollection<LiveAndRecordInfo> FilteredVideos =>
            string.IsNullOrWhiteSpace(SearchText)
                ? Videos
                : new ObservableCollection<LiveAndRecordInfo>(
                    Videos.Where(v =>
                        v.LiveRecordName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        v.TeacherName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        public VideoDownloadListViewModel(
            List<LiveAndRecordInfo> liveAndRecordInfos,
            VideoDownloadService downloadService,
            ILearnApiService iLearnApiService,
            ISnackbarService snackbarService,
            AppConfig appConfig)
        {
            _downloadService = downloadService;
            _iLearnApiService = iLearnApiService;
            _snackbarService = snackbarService;
            _appConfig = appConfig;

            Videos = new ObservableCollection<LiveAndRecordInfo>(liveAndRecordInfos ?? []);

            foreach (var video in Videos)
            {
                video.PropertyChanged += OnVideoPropertyChanged;
            }

            RefreshSelectedDownloadCount();
        }

        partial void OnSearchTextChanged(string value)
        {
            OnPropertyChanged(nameof(FilteredVideos));
        }

        partial void OnIsAllHdmiSelectedChanged(bool value)
        {
            if (_isUpdatingAllSelected)
                return;

            foreach (var video in Videos)
            {
                video.PropertyChanged -= OnVideoPropertyChanged;
            }

            foreach (var video in Videos)
            {
                video.IsHdmiSelected = value;
            }

            foreach (var video in Videos)
            {
                video.PropertyChanged += OnVideoPropertyChanged;
            }

            RefreshSelectedDownloadCount();
        }

        partial void OnIsAllTeacherSelectedChanged(bool value)
        {
            if (_isUpdatingAllSelected)
                return;

            foreach (var video in Videos)
            {
                video.PropertyChanged -= OnVideoPropertyChanged;
            }

            foreach (var video in Videos)
            {
                video.IsTeacherSelected = value;
            }

            foreach (var video in Videos)
            {
                video.PropertyChanged += OnVideoPropertyChanged;
            }

            RefreshSelectedDownloadCount();
        }

        partial void OnSelectedDownloadCountChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedDownloadText));
        }

        private void OnVideoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LiveAndRecordInfo.IsHdmiSelected):
                    _isUpdatingAllSelected = true;

                    try
                    {
                        var allHdmiSelected = Videos.All(v => v.IsHdmiSelected);
                        if (IsAllHdmiSelected != allHdmiSelected)
                        {
                            IsAllHdmiSelected = allHdmiSelected;
                        }
                    }
                    finally
                    {
                        _isUpdatingAllSelected = false;
                    }

                    RefreshSelectedDownloadCount();
                    break;
                case nameof(LiveAndRecordInfo.IsTeacherSelected):
                    _isUpdatingAllSelected = true;

                    try
                    {
                        var allTeacherSelected = Videos.All(v => v.IsTeacherSelected);
                        if (IsAllTeacherSelected != allTeacherSelected)
                        {
                            IsAllTeacherSelected = allTeacherSelected;
                        }
                    }
                    finally
                    {
                        _isUpdatingAllSelected = false;
                    }

                    RefreshSelectedDownloadCount();
                    break;
            }
        }

        [RelayCommand]
        private async Task DownloadSelected()
        {
            if (IsPreparingDownloads)
                return;

            var selections = Videos
                .Where(v => !string.IsNullOrWhiteSpace(v.ResourceId))
                .SelectMany(v => new[]
                {
                    new VideoSelection(v, "HDMI", v.IsHdmiSelected),
                    new VideoSelection(v, "教师", v.IsTeacherSelected)
                })
                .Where(selection => selection.Selected)
                .ToList();

            if (selections.Count == 0)
            {
                ShowSnackbar("请选择要下载的视频", "没有选中任何视频进行下载", ControlAppearance.Info);
                return;
            }

            IsPreparingDownloads = true;
            var queued = 0;
            var failed = 0;

            try
            {
                foreach (var selection in selections)
                {
                    if (await DownloadVideoAsync(selection.Video, selection.Perspective))
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
            ShowSnackbar(
                "下载任务已添加",
                $"成功加入队列 {queued} 个，失败 {failed} 个",
                failed == 0 ? ControlAppearance.Success : ControlAppearance.Caution);
        }

        private async Task<bool> DownloadVideoAsync(LiveAndRecordInfo video, string perspective)
        {
            try
            {
                var folder = _appConfig.DownloadPath;
                Directory.CreateDirectory(folder);

                var videoInfo = await _iLearnApiService.GetVideoInfoAsync(video.ResourceId);
                await DownloadSubtitleAsync(videoInfo);

                var videoSource = perspective == "HDMI"
                    ? videoInfo.VideoList.ElementAtOrDefault(1)
                    : videoInfo.VideoList.ElementAtOrDefault(0);

                if (string.IsNullOrWhiteSpace(videoSource?.VideoPath))
                    return false;

                var fileName = FileNameService.BuildVideoFileName(video.LiveRecordName, perspective);
                return await _downloadService.StartDownloadAsync(videoSource.VideoPath, fileName, folder, perspective);
            }
            catch (Exception ex)
            {
                ShowSnackbar(
                    "下载失败",
                    $"无法下载视频 {video.LiveRecordName}: {ex.Message}",
                    ControlAppearance.Danger);
                return false;
            }
        }

        private async Task DownloadSubtitleAsync(VideoInfo videoInfo)
        {
            if (string.IsNullOrWhiteSpace(videoInfo.PhaseUrl))
                return;

            var fileName = FileNameService.BuildSubtitleFileName(videoInfo.ResourceName);
            var folder = Path.Combine(_appConfig.DownloadPath, "Subtitles");

            await _downloadService.StartDownloadAsync(videoInfo.PhaseUrl, fileName, folder, "字幕");
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
            SelectedDownloadCount = Videos.Count(v => v.IsHdmiSelected) + Videos.Count(v => v.IsTeacherSelected);
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

        private sealed record VideoSelection(LiveAndRecordInfo Video, string Perspective, bool Selected);
    }
}
