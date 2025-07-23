using iLearn.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace iLearn.ViewModels.Pages
{
    public partial class LocalVideoViewModel : ObservableObject
    {
        private readonly ISnackbarService _snackbarService;
        private readonly AppConfig _appConfig;

        [ObservableProperty]
        private ObservableCollection<LocalVideoFile> _localVideos = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "全部";

        [ObservableProperty]
        private bool _isLoading = false;

        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "全部", "HDMI视角", "教师视角"
        };

        public ObservableCollection<LocalVideoFile> FilteredVideos
        {
            get
            {
                var filtered = LocalVideos.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    filtered = filtered.Where(v =>
                        v.CourseName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                        v.FileName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
                }

                filtered = SelectedFilter switch
                {
                    "HDMI视角" => filtered.Where(v => v.Perspective.Contains("HDMI")),
                    "教师视角" => filtered.Where(v => v.Perspective.Contains("教师") || v.Perspective.Contains("teacher")),
                    _ => filtered
                };

                return new ObservableCollection<LocalVideoFile>(
                    filtered.OrderByDescending(v => v.RecordDate)
                           .ThenBy(v => v.CourseName));
            }
        }

        public LocalVideoViewModel(ISnackbarService snackbarService, AppConfig appConfig)
        {
            _snackbarService = snackbarService;
            _appConfig = appConfig;
            LoadLocalVideos();
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
        private async Task LoadLocalVideos()
        {
            IsLoading = true;
            try
            {
                await Task.Run(() =>
                {
                    var videos = new List<LocalVideoFile>();
                    var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv" };

                    var downloadsPath = _appConfig.DownloadPath;
                    if (Directory.Exists(downloadsPath))
                    {
                        var videoFiles = Directory.GetFiles(downloadsPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLower()));

                        foreach (var file in videoFiles)
                        {
                            videos.Add(LocalVideoFile.FromFileName(file));
                        }
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        LocalVideos.Clear();
                        foreach (var video in videos)
                        {
                            LocalVideos.Add(video);
                        }
                    });
                });

                OnPropertyChanged(nameof(FilteredVideos));
                ShowSnackbar("加载完成", $"找到 {LocalVideos.Count} 个本地视频文件", ControlAppearance.Success);
            }
            catch (Exception ex)
            {
                ShowSnackbar("加载失败", $"无法加载本地视频: {ex.Message}", ControlAppearance.Danger);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task OpenVideo(LocalVideoFile video)
        {
            if (video == null || !File.Exists(video.FullPath))
            {
                ShowSnackbar("文件不存在", "视频文件已被移动或删除", ControlAppearance.Danger);
                return;
            }

            try
            {
                _ = OpenLocalVideoAsync(video);
                ShowSnackbar("正在打开", $"正在使用默认程序打开 {video.FileName}", ControlAppearance.Info);
            }
            catch (Exception ex)
            {
                ShowSnackbar("打开失败", $"无法打开视频文件: {ex.Message}", ControlAppearance.Danger);
            }
        }

        [RelayCommand]
        private async Task OpenFileLocation(LocalVideoFile video)
        {
            if (video == null || !File.Exists(video.FullPath))
            {
                ShowSnackbar("文件不存在", "视频文件已被移动或删除", ControlAppearance.Danger);
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"/select,\"{video.FullPath}\"");
            }
            catch (Exception ex)
            {
                ShowSnackbar("打开失败", $"无法打开文件位置: {ex.Message}", ControlAppearance.Danger);
            }
        }

        [RelayCommand]
        private async Task DeleteVideo(LocalVideoFile video)
        {
            if (video == null || !File.Exists(video.FullPath))
            {
                ShowSnackbar("文件不存在", "视频文件已被移动或删除", ControlAppearance.Danger);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除视频文件吗？\n\n{video.FileName}",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(video.FullPath);
                    LocalVideos.Remove(video);
                    OnPropertyChanged(nameof(FilteredVideos));
                    ShowSnackbar("删除成功", "视频文件已删除", ControlAppearance.Success);
                }
                catch (Exception ex)
                {
                    ShowSnackbar("删除失败", $"无法删除文件: {ex.Message}", ControlAppearance.Danger);
                }
            }
        }

        [RelayCommand]
        private void Search()
        {
            OnPropertyChanged(nameof(FilteredVideos));
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

        private async Task OpenLocalVideoAsync(LocalVideoFile video)
        {
            var uri = new Uri("pack://application:,,,/Assets/VideoPlayer.html");
            var streamInfo = Application.GetResourceStream(uri);

            using var reader = new StreamReader(streamInfo.Stream);
            var content = await reader.ReadToEndAsync();

            content = content.Replace("_LEFTVIDEO_", video.FullPath);

            var partnerVideo = video.GetPartnerVideo();
            if (partnerVideo != null)
                content = content.Replace("_RIGHTVIDEO_", partnerVideo.FullPath);

            string tempFile = Path.Combine(Path.GetTempPath(), $"video_local_{Guid.NewGuid()}.html");
            await File.WriteAllTextAsync(tempFile, content);

            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            };
            process.Start();
        }
    }
}