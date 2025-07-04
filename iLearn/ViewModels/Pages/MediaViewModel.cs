using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using Wpf.Ui;

namespace iLearn.ViewModels.Pages
{
    public partial class MediaViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LiveAndRecordInfo> _mediaItems = new();

        [ObservableProperty]
        private ClassInfo? _currentCourse;

        private readonly ILearnApiService _ilearnApiService;
        private readonly INavigationService _navigationService;

        public MediaViewModel(ILearnApiService ilearnApiService, INavigationService navigationService, List<LiveAndRecordInfo> liveAndRecordInfos)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            IsActive = true;
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

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
        }

        private async Task LoadData()
        {
            MediaItems.Clear();
            var info = await _ilearnApiService.GetLiveAndRecordInfoAsync(CurrentCourse.TermId, CurrentCourse.ClassId);

            MediaItems = new ObservableCollection<LiveAndRecordInfo>(info);
        }
    }
}