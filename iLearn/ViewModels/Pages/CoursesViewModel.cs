using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
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
        private ObservableCollection<CourseModel> _myCourses;

        [ObservableProperty]
        private string _courseCode = string.Empty;

        private readonly ILearnApiService _ilearnApiService;
        private readonly ISnackbarService _snackbarService;

        public CoursesViewModel(ILearnApiService ilearnApiService, ISnackbarService snackbarService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
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
                var course = new CourseModel(classInfo.Name, classInfo.TeacherName, classInfo.Cover);
                MyCourses.Add(course);
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
    }

    public class CourseModel
    {
        public string CourseName { get; set; }
        public string TeacherName { get; set; }
        public string ImageUrl { get; set; }

        public CourseModel(string name, string teacher, string imageUrl)
        {
            CourseName = name;
            TeacherName = teacher;
            ImageUrl = imageUrl;
        }
    }
}