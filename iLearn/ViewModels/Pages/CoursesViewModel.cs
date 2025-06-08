using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iLearn.Services;
using System.Collections.ObjectModel;

namespace iLearn.ViewModels.Pages
{
    public partial class CoursesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> _termsOptions;

        [ObservableProperty]
        private string? _selectedTerm;

        [ObservableProperty]
        private ObservableCollection<CourseModel> _myCourses;

        [ObservableProperty]
        private string _searchText = string.Empty;

        private readonly ILearnApiService _ilearnApiService;

        public CoursesViewModel(ILearnApiService ilearnApiService)
        {
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            Init().ConfigureAwait(false);
        }

        private async Task Init()
        {
            TermsOptions = [];

            var terms = await _ilearnApiService.GetTermsAsync();
            foreach (var term in terms)
            {
                TermsOptions.Add($"{term.Year}-{int.Parse(term.Year) + 1}学年{term.Name}");
            }

            SelectedTerm = TermsOptions.FirstOrDefault();

            MyCourses = [];

            var classes = await _ilearnApiService.GetClassesAsync(terms.FirstOrDefault()?.Year, terms.FirstOrDefault()?.Num);
            foreach (var classInfo in classes)
            {
                var course = new CourseModel(classInfo.Name, classInfo.TeacherName, classInfo.Cover);
                MyCourses.Add(course);
            }
        }

        [RelayCommand]
        private void JoinCourse()
        {

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