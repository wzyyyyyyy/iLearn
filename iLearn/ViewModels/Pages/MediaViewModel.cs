using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Services;
using iLearn.ViewModels.Windows;
using System.Collections.ObjectModel;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Pages
{
    public partial class MediaViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LiveAndRecordInfo> _mediaItems = new();

        [ObservableProperty]
        private ClassInfo? _currentCourse;

        private readonly ILearnApiService _ilearnApiService;
        private readonly INavigationService _navigationService;
        private readonly WindowsManagerService _windowsManagerService;
        private readonly ISnackbarService _snackbarService;

        public MediaViewModel(ILearnApiService ilearnApiService, INavigationService navigationService, List<LiveAndRecordInfo> liveAndRecordInfos, WindowsManagerService windowsManagerService, ISnackbarService snackbarService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _windowsManagerService = windowsManagerService ?? throw new ArgumentNullException(nameof(windowsManagerService));
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));

            WeakReferenceMessenger.Default.Register<CourseMessage>(this, async (r, m) =>
            {
                CurrentCourse = m.classInfo;
                await LoadData();
                liveAndRecordInfos.Clear();
                liveAndRecordInfos.AddRange(MediaItems);
            });
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadData();

            var filteredItems = MediaItems.Where(item =>
                (item.LiveRecordName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                (item.TeacherName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();

            MediaItems.Clear();
            foreach (var item in filteredItems)
            {
                MediaItems.Add(item);
            }
        }

        [RelayCommand]
        private async Task OpenMediaAsync(LiveAndRecordInfo media)
        {
            if (media.ResourceId is null)
            {
                _snackbarService.Show(
                "错误",
                "该资源没有视频可供播放",
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.CalendarError16),
                TimeSpan.FromSeconds(3)
                );
                return;
            }

            _snackbarService.Show(
                "正在加载视频",
                "请稍等，正在加载视频信息...",
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.CalendarClock16),
                TimeSpan.FromSeconds(3)
            );

            var videoInfo = await _ilearnApiService.GetVideoInfoAsync(media.ResourceId);

            var uri = new Uri("pack://application:,,,/Assets/VideoPlayer.html");
            var streamInfo = Application.GetResourceStream(uri);

            using var reader = new StreamReader(streamInfo.Stream);
            var content = await reader.ReadToEndAsync();

            content = content.Replace("_LEFTVIDEO_", videoInfo.VideoList[0].VideoPath)
                  .Replace("_RIGHTVIDEO_", videoInfo.VideoList[1].VideoPath)
                  .Replace("_SUBTITLE_", videoInfo.PhaseUrl);


            string tempFile = Path.Combine(Path.GetTempPath(), $"video_preview_{Guid.NewGuid()}.html");
            await File.WriteAllTextAsync(tempFile, content);

            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true 
            };
            process.Start();
        }

        private async Task LoadData()
        {
            MediaItems.Clear();
            var info = await _ilearnApiService.GetLiveAndRecordInfoAsync(CurrentCourse.TermId, CurrentCourse.ClassId);

            MediaItems = new ObservableCollection<LiveAndRecordInfo>(info);
        }
    }
}