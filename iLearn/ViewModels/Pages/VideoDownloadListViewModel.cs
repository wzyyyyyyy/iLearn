using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace iLearn.ViewModels.Pages
{
    public partial class VideoDownloadListViewModel : ObservableObject
    {
        private readonly VideoDownloadService _downloadService;
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
            VideoDownloadService downloadService)
        {
            _downloadService = downloadService;

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

            var selectedHdmiVideos = Videos.Where(v => v.IsHdmiSelected).ToList();
            foreach (var video in selectedHdmiVideos)
            {
                DownloadVideoAsync(video, "hdmi").ConfigureAwait(false);
            }

            var selectedTeacherVideos = Videos.Where(v => v.IsTeacherSelected).ToList();
            foreach (var video in selectedTeacherVideos)
            {
                DownloadVideoAsync(video, "teacher").ConfigureAwait(false);
            }
        }

        private async Task DownloadVideoAsync(LiveAndRecordInfo video, string perspective)
        {
            var folder = Path.Combine(System.Environment.CurrentDirectory, "Downloads");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var perspectiveSuffix = perspective switch
            {
                "hdmi" => "_HDMI",
                "teacher" => "_教师",
                _ => ""
            };

            var fileName = SanitizeFileName(video.LiveRecordName) + perspectiveSuffix + ".mp4";
            var filePath = Path.Combine(folder, fileName);

            try
            {
                //await _downloadService.DownloadFileAsync(video, filePath, perspective);
            }
            catch (System.Exception ex)
            {
                // 处理下载异常
            }
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
    }
}