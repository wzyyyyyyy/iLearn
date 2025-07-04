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
        [NotifyPropertyChangedFor(nameof(FilteredVideos))]
        private bool _isAllSelected;

        public ObservableCollection<LiveAndRecordInfo> FilteredVideos =>
            string.IsNullOrWhiteSpace(SearchText)
                ? Videos
                : new ObservableCollection<LiveAndRecordInfo>(
                    Videos.Where(v => v.LiveRecordName.Contains(SearchText) ||
                                     v.TeacherName.Contains(SearchText)));

        public VideoDownloadListViewModel(
            System.Collections.Generic.List<LiveAndRecordInfo> liveAndRecordInfos,
            VideoDownloadService downloadService)
        {
            _downloadService = downloadService;

            Videos = new ObservableCollection<LiveAndRecordInfo>(liveAndRecordInfos ?? new System.Collections.Generic.List<LiveAndRecordInfo>());

            // 订阅每个视频项的选择状态变化
            foreach (var video in Videos)
            {
                video.PropertyChanged += OnVideoPropertyChanged;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            OnPropertyChanged(nameof(FilteredVideos));
        }

        partial void OnIsAllSelectedChanged(bool value)
        {
            if (_isUpdatingAllSelected)
                return;

            // 暂时取消订阅，避免循环触发
            foreach (var video in Videos)
            {
                video.PropertyChanged -= OnVideoPropertyChanged;
            }

            foreach (var video in Videos)
            {
                video.IsSelected = value;
            }

            foreach (var video in Videos)
            {
                video.PropertyChanged += OnVideoPropertyChanged;
            }
        }

        private void OnVideoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LiveAndRecordInfo.IsSelected))
            {
                _isUpdatingAllSelected = true;

                try
                {
                    var allSelected = Videos.All(v => v.IsSelected);

                    if (IsAllSelected != allSelected)
                    {
                        IsAllSelected = allSelected;
                    }
                }
                finally
                {
                    _isUpdatingAllSelected = false;
                }
            }
        }

        [RelayCommand]
        private async Task DownloadSelectedAsync()
        {
            var selectedVideos = Videos.Where(v => v.IsSelected).ToList();
            if (!selectedVideos.Any())
                return;

            foreach (var video in selectedVideos)
            {
                await DownloadVideoAsync(video);
            }
        }

        [RelayCommand]
        private async Task DownloadSingleAsync(LiveAndRecordInfo video)
        {
            if (video == null)
                return;

            await DownloadVideoAsync(video);
        }

        private async Task DownloadVideoAsync(LiveAndRecordInfo video)
        {
            var folder = Path.Combine(System.Environment.CurrentDirectory, "Downloads");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = SanitizeFileName(video.LiveRecordName) + ".mp4";
            var filePath = Path.Combine(folder, fileName);
            MessageBox.Show($"正在下载: {video.LiveRecordName} 到 {filePath}", "下载开始", MessageBoxButton.OK, MessageBoxImage.Information);
            try
            {
                await _downloadService.DownloadFileAsync(video, filePath);
            }
            catch (System.Exception ex)
            {

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