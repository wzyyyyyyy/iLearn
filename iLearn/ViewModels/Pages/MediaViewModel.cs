using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Services;
using iLearn.ViewModels.Windows;
using System.Collections.ObjectModel;
using Wpf.Ui;

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

        public MediaViewModel(ILearnApiService ilearnApiService, INavigationService navigationService, List<LiveAndRecordInfo> liveAndRecordInfos, WindowsManagerService windowsManagerService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _windowsManagerService = windowsManagerService ?? throw new ArgumentNullException(nameof(windowsManagerService));

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
        private void OpenMedia(LiveAndRecordInfo media)
        {
            MessageBox.Show($"正在打开: {media.LiveRecordName}", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            _windowsManagerService.Show<VideoPlayerViewModel>();
        }

        private async Task LoadData()
        {
            MediaItems.Clear();
            var info = await _ilearnApiService.GetLiveAndRecordInfoAsync(CurrentCourse.TermId, CurrentCourse.ClassId);

            MediaItems = new ObservableCollection<LiveAndRecordInfo>(info);
        }
    }
}