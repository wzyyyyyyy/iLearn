using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

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

        public MediaViewModel(ILearnApiService ilearnApiService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            IsActive = true;

            WeakReferenceMessenger.Default.Register<CourseMessage>(this, (r, m) => {
                CurrentCourse = m.classInfo;
                LoadData().ConfigureAwait(false);
            });
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadData();

            var filteredItems = MediaItems.Where(item =>
                (item.CourseName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                (item.ResourceId?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) == true))
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