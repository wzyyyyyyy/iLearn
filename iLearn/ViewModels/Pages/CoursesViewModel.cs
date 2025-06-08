using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.Services;
using iLearn.Views.Pages;
using System.Collections.ObjectModel;
using System.Windows.Navigation;
using Wpf.Ui;
using Wpf.Ui.Controls;

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

        [ObservableProperty]
        private string _courseCode = string.Empty;

        private readonly ILearnApiService _ilearnApiService;
        private readonly ISnackbarService _snackbarService;
        private readonly INavigationService _navigationService;

        public CoursesViewModel(ILearnApiService ilearnApiService, ISnackbarService snackbarService,INavigationService navigationService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
            if (string.IsNullOrWhiteSpace(CourseCode))
            {
                _snackbarService.Show(
                "错误",
                "请输入课程码",
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.CalendarError16),
                TimeSpan.FromSeconds(3)
                );
                return;
            }

            _ilearnApiService.JoinCourse(CourseCode)
                .ContinueWith(task =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!task.IsCompletedSuccessfully)
                        {
                            _snackbarService.Show(
                             "错误",
                             "加入失败！",
                            ControlAppearance.Caution,
                            new SymbolIcon(SymbolRegular.CalendarError16),
                            TimeSpan.FromSeconds(3)
                            );
                        }
                        else
                        {
                            _snackbarService.Show(
                            "Info",
                            task.Result,
                           ControlAppearance.Info,
                           new SymbolIcon(SymbolRegular.Connected16),
                           TimeSpan.FromSeconds(3)
                           );
                        }
                        Init();
                    });
                });
        }

        [RelayCommand]
        public void TermSelectionChanged()
        {
            LoadCoursesAsync(SelectedTerm).ConfigureAwait(false);
        }

        [RelayCommand]
        private void CourseSelected(ClassInfo course)
        {
            WeakReferenceMessenger.Default.Send(new CourseMessage { classInfo = course});
            _navigationService.Navigate(typeof(MediaPage));
        }
    }
}