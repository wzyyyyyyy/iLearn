using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iLearn.Models;
using iLearn.Services;
using System.Collections.ObjectModel;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Windows
{
    public partial class JoinCourseViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

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
        private int _pageSize = 50;

        [ObservableProperty]
        private string _paginationInfo = "";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isSearching = false;

        private readonly ISnackbarService _snackbarService;
        private readonly CourseDateService _courseDateService;
        private readonly ILearnApiService _ilearnApiService;
        private CancellationTokenSource? _searchCancellationTokenSource;

        public JoinCourseViewModel(ISnackbarService snackbarService, CourseDateService courseDateService, ILearnApiService ilearnApiService)
        {
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _courseDateService = courseDateService ?? throw new ArgumentNullException(nameof(courseDateService));
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));

            _ = Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            await LoadCoursesAsync();
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _searchCancellationTokenSource.Token;

            IsSearching = true;
            CurrentPage = 1;

            try
            {
                await LoadCoursesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, do nothing
            }
            finally
            {
                IsSearching = false;
            }
        }

        [RelayCommand]
        private async Task FirstPageAsync()
        {
            if (CurrentPage == 1) return;
            
            CurrentPage = 1;
            await LoadCoursesAsync();
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage <= 1) return;
            
            CurrentPage--;
            await LoadCoursesAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages) return;
            
            CurrentPage++;
            await LoadCoursesAsync();
        }

        [RelayCommand]
        private async Task LastPageAsync()
        {
            if (CurrentPage == TotalPages) return;
            
            CurrentPage = TotalPages;
            await LoadCoursesAsync();
        }

        [RelayCommand]
        private async Task JoinCourseAsync(LocalCourseData course)
        {
            if (course == null) return;

            try
            {
                IsLoading = true;

                _snackbarService.Show(
                    "正在加入课程",
                    $"正在加入课程: {course.CourseName}",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.BookAdd24),
                    TimeSpan.FromSeconds(2)
                );

                var result = await _ilearnApiService.JoinCourse(course.CourseId);

                if (!string.IsNullOrEmpty(result))
                {
                    _snackbarService.Show(
                        "加入成功",
                        $"成功加入课程: {course.CourseName}",
                        ControlAppearance.Success,
                        new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                        TimeSpan.FromSeconds(3)
                    );
                }
                else
                {
                    _snackbarService.Show(
                        "加入失败",
                        "课程加入失败，请稍后重试",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.ErrorCircle24),
                        TimeSpan.FromSeconds(3)
                    );
                }
            }
            catch (Exception ex)
            {
                _snackbarService.Show(
                    "加入失败",
                    $"加入课程时发生错误: {ex.Message}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5)
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshCoursesAsync()
        {
            SearchQuery = string.Empty;
            CurrentPage = 1;
            await LoadCoursesAsync();

            _snackbarService.Show(
                "刷新完成",
                "课程列表已刷新",
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.ArrowSync24),
                TimeSpan.FromSeconds(2)
            );
        }

        private async Task LoadCoursesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                
                var skip = (CurrentPage - 1) * PageSize;
                List<LocalCourseData> courses;
                int totalCount;

                await Task.Run(() =>
                {
                    if (string.IsNullOrWhiteSpace(SearchQuery))
                    {
                        courses = _courseDateService.GetLocalCourseDatas(skip, PageSize);
                        totalCount = _courseDateService.GetCoursesCount();
                    }
                    else
                    {
                        courses = _courseDateService.SearchCourses(SearchQuery, skip, PageSize);
                        totalCount = _courseDateService.GetSearchResultsCount(SearchQuery);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        PagedCourses.Clear();
                        foreach (var course in courses)
                        {
                            PagedCourses.Add(course);
                        }

                        TotalCourses = totalCount;
                        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
                        
                        if (CurrentPage > TotalPages)
                        {
                            CurrentPage = TotalPages;
                        }

                        UpdatePaginationInfo();
                    });
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _snackbarService.Show(
                    "加载失败",
                    $"加载课程列表时发生错误: {ex.Message}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5)
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdatePaginationInfo()
        {
            if (TotalCourses == 0)
            {
                PaginationInfo = "没有找到课程";
            }
            else
            {
                var startIndex = (CurrentPage - 1) * PageSize + 1;
                var endIndex = Math.Min(CurrentPage * PageSize, TotalCourses);
                PaginationInfo = $"第 {startIndex}-{endIndex} 项，共 {TotalCourses} 项 (第 {CurrentPage} 页，共 {TotalPages} 页)";
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _searchCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await App.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            CurrentPage = 1;
                            await LoadCoursesAsync(cancellationToken);
                        });
                    }
                }
                catch (OperationCanceledException)
                {

                }
            });
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;
        public bool HasMultiplePages => TotalPages > 1;

        partial void OnCurrentPageChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(HasMultiplePages));
            UpdatePaginationInfo();
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(HasMultiplePages));
            UpdatePaginationInfo();
        }

        partial void OnTotalCoursesChanged(int value)
        {
            UpdatePaginationInfo();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchCancellationTokenSource?.Cancel();
                _searchCancellationTokenSource?.Dispose();
                _courseDateService?.Dispose();
            }
        }

        ~JoinCourseViewModel()
        {
            Dispose(false);
        }
    }
}