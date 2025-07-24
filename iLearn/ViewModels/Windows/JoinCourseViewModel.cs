using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Windows
{
    public partial class JoinCourseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<LocalCourseData> _availableCourses = new();

        [ObservableProperty]
        private ObservableCollection<LocalCourseData> _filteredCourses = new();

        [ObservableProperty]
        private ObservableCollection<LocalCourseData> _pagedCourses = new();

        [ObservableProperty]
        private LocalCourseData? _selectedCourse;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalCourses = 0;

        [ObservableProperty]
        private int _pageSize = 20;

        [ObservableProperty]
        private string _paginationInfo = "";

        private readonly ISnackbarService _snackbarService;
        private readonly CourseDateService _courseDateService;
        private readonly ILearnApiService _ilearnApiService;

        public JoinCourseViewModel(ISnackbarService snackbarService, CourseDateService courseDateService, ILearnApiService ilearnApiService)
        {
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _courseDateService = courseDateService ?? throw new ArgumentNullException(nameof(courseDateService));
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));
            LoadData();
        }

        private void LoadData()
        {
            AvailableCourses = new(_courseDateService.GetLocalCourseDatas());
            ApplyFiltersAndPagination();
        }

        private void ApplyFiltersAndPagination()
        {
            var filteredItems = AvailableCourses.Where(course =>
                string.IsNullOrWhiteSpace(SearchQuery) ||
                (course.CourseName ?? "").Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                (course.CourseId ?? "").Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                (course.TeacherName ?? "").Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                (course.Term ?? "").Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
            ).ToList();


            FilteredCourses.Clear();
            foreach (var item in filteredItems)
            {
                FilteredCourses.Add(item);
            }

            TotalCourses = FilteredCourses.Count;
            TotalPages = (int)Math.Ceiling((double)TotalCourses / PageSize);
            
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }
            else if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            var pagedItems = FilteredCourses
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            PagedCourses.Clear();
            foreach (var item in pagedItems)
            {
                PagedCourses.Add(item);
            }

            UpdatePaginationInfo();
        }

        private void UpdatePaginationInfo()
        {
            if (TotalCourses == 0)
            {
                PaginationInfo = "没有找到课程";
            }
            else
            {
                int startItem = (CurrentPage - 1) * PageSize + 1;
                int endItem = Math.Min(CurrentPage * PageSize, TotalCourses);
                PaginationInfo = $"显示 {startItem}-{endItem} 项，共 {TotalCourses} 项";
            }
        }

        [RelayCommand]
        private void Search()
        {
            CurrentPage = 1;
            ApplyFiltersAndPagination();
        }

        [RelayCommand]
        private void FirstPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                ApplyFiltersAndPagination();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                ApplyFiltersAndPagination();
            }
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                ApplyFiltersAndPagination();
            }
        }

        [RelayCommand]
        private void LastPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                ApplyFiltersAndPagination();
            }
        }

        [RelayCommand]
        private async Task JoinCourseAsync(LocalCourseData course)
        {
            if (course == null)
                return;

            SelectedCourse = course;
            var inviteCode = GenerateInviteCode(course);

            try
            {
                await _ilearnApiService.JoinCourse(inviteCode);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                    _snackbarService.Show(
                        "加入成功",
                        $"已成功加入课程：{course.CourseName}",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle16),
                        TimeSpan.FromSeconds(3))
                );
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    _snackbarService.Show(
                        "加入失败",
                        $"无法加入课程：{course.CourseName}\n{ex.Message}",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.ErrorCircle16),
                        TimeSpan.FromSeconds(5))
                );
            }
        }


        [RelayCommand]
        private void RefreshCourses()
        {
            LoadData();
            
            _snackbarService.Show(
                "刷新完成",
                "课程列表已更新",
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.ArrowClockwise16),
                TimeSpan.FromSeconds(2)
            );
        }

        private string GenerateInviteCode(LocalCourseData localCourseData)
        {
            return $"{localCourseData.Term}{localCourseData.CourseId}{localCourseData.SectionId}";
        }

        partial void OnSearchQueryChanged(string value)
        {
            Search();
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;
        public bool HasMultiplePages => TotalPages > 1;

        partial void OnCurrentPageChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(HasMultiplePages));
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(HasMultiplePages));
        }
    }
}