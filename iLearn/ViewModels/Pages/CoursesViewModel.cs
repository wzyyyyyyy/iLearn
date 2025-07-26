using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Services;
using iLearn.ViewModels.Windows;
using iLearn.Views.Pages;
using System.Collections.ObjectModel;
using Wpf.Ui;

namespace iLearn.ViewModels.Pages
{
    public partial class CoursesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<TermInfo> _termsOptions;

        [ObservableProperty]
        private TermInfo? _selectedTerm;

        [ObservableProperty]
        private ObservableCollection<ClassInfo> _myCourses;

        private readonly ILearnApiService _ilearnApiService;
        private readonly ISnackbarService _snackbarService;
        private readonly INavigationService _navigationService;
        private readonly WindowsManagerService _windowsManagerService;

        public CoursesViewModel(ILearnApiService ilearnApiService, ISnackbarService snackbarService, INavigationService navigationService, WindowsManagerService windowsManagerService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _windowsManagerService = windowsManagerService ?? throw new ArgumentNullException(nameof(windowsManagerService));
            Init().ConfigureAwait(false);
        }

        private async Task Init()
        {
            TermsOptions = [];

            var terms = await _ilearnApiService.GetTermsAsync();
            foreach (var term in terms)
            {
                TermsOptions.Add(term);
            }

            SelectedTerm = TermsOptions.FirstOrDefault();

            MyCourses = [];

            if (SelectedTerm != null)
            {
                await LoadCoursesAsync(SelectedTerm);
            }
        }

        private async Task LoadCoursesAsync(TermInfo term)
        {
            var classes = await _ilearnApiService.GetClassesAsync(term.Year, term.Num);
            MyCourses.Clear();
            foreach (var classInfo in classes)
            {
                MyCourses.Add(classInfo);
            }
        }

        [RelayCommand]
        private void JoinCourse()
        {
            _windowsManagerService.Show<JoinCourseViewModel>();
        }

        [RelayCommand]
        public void TermSelectionChanged()
        {
            LoadCoursesAsync(SelectedTerm).ConfigureAwait(false);
        }

        [RelayCommand]
        private void CourseSelected(ClassInfo course)
        {
            _navigationService.Navigate(typeof(MediaPage));
            WeakReferenceMessenger.Default.Send(new CourseMessage { classInfo = course });
        }
    }
}