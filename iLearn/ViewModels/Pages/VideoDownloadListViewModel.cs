﻿using iLearn.Models;
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

        public ObservableCollection<LiveAndRecordInfo> FilteredVideos =>
            string.IsNullOrWhiteSpace(SearchText)
                ? Videos
                : new ObservableCollection<LiveAndRecordInfo>(
                    Videos.Where(v => v.LiveRecordName.Contains(SearchText) ||
                                     v.TeacherName.Contains(SearchText)));

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

            Videos = new ObservableCollection<LiveAndRecordInfo>(liveAndRecordInfos ?? new List<LiveAndRecordInfo>());

            foreach (var video in Videos)
            {
                video.PropertyChanged += OnVideoPropertyChanged;
            }
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
        }

        private void OnVideoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LiveAndRecordInfo.IsHdmiSelected))
            {
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
            }
            else if (e.PropertyName == nameof(LiveAndRecordInfo.IsTeacherSelected))
            {
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
            }
        }

        [RelayCommand]
        private void DownloadSelected()
        {
            var hdmiSelectedVideos = Videos.Where(v => v.IsHdmiSelected && v.ResourceId is not null).ToList();
            var teacherSelectedVideos = Videos.Where(v => v.IsTeacherSelected && v.ResourceId is not null).ToList();

            var totalSelectedCount = hdmiSelectedVideos.Count + teacherSelectedVideos.Count;

            if (totalSelectedCount == 0)
            {
                ShowSnackbar("请选择要下载的视频", "没有选中任何视频进行下载", ControlAppearance.Info);
                return;
            }

            ShowSnackbar(
                "开始下载",
                $"正在准备下载 {totalSelectedCount} 个文件",
                ControlAppearance.Success);

            foreach (var video in hdmiSelectedVideos)
            {
                DownloadVideoAsync(video, "hdmi").ConfigureAwait(false);
            }

            foreach (var video in teacherSelectedVideos)
            {
                DownloadVideoAsync(video, "teacher").ConfigureAwait(false);
            }
        }

        private async Task DownloadVideoAsync(LiveAndRecordInfo video, string perspective)
        {
            var folder = _appConfig.DownloadPath;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            try
            {
                var videoInfo = await _iLearnApiService.GetVideoInfoAsync(video.ResourceId);

                DownloadSubtitle(videoInfo);

                string? url;
                string? perspectiveSuffix;

                switch (perspective)
                {
                    case "hdmi":
                        perspectiveSuffix = "_HDMI";
                        url = videoInfo.VideoList[1].VideoPath;
                        break;
                    case "teacher":
                        perspectiveSuffix = "_教师";
                        url = videoInfo.VideoList[0].VideoPath;
                        break;
                    default:
                        return; // 无效的视角
                }

                var fileName = SanitizeFileName(video.LiveRecordName) + perspectiveSuffix + ".mp4";

                _ = _downloadService.StartDownloadAsync(url, fileName, folder).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ShowSnackbar(
                    "下载失败",
                    $"无法下载视频 {video.LiveRecordName}: {ex.Message}",
                    ControlAppearance.Danger);
            }
        }

        private void DownloadSubtitle(VideoInfo videoInfo)
        {
            var url = videoInfo.PhaseUrl;
            var fileName = SanitizeFileName(videoInfo.ResourceName) + ".vtt";
            var folder = Path.Combine(_appConfig.DownloadPath, "Subtitles");

            _ = _downloadService.StartDownloadAsync(url, fileName, folder).ConfigureAwait(false);
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "未命名";

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        private void ShowSnackbar(string title, string message, ControlAppearance appearance)
        {
            var icon = appearance switch
            {
                ControlAppearance.Success => new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                ControlAppearance.Danger => new SymbolIcon(SymbolRegular.ErrorCircle24),
                ControlAppearance.Info => new SymbolIcon(SymbolRegular.Info24),
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
    }
}