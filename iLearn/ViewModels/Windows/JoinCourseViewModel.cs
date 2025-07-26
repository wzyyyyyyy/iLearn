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

        private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
        private CancellationTokenSource? _currentOperationCts;
        private readonly object _dataLock = new object();

        private List<LocalCourseData> _allCoursesCache = new();
        private Dictionary<string, List<LocalCourseData>> _searchCache = new();
        private string _lastSearchQuery = string.Empty;

        private Timer? _searchTimer;
        private readonly object _searchLock = new object();

        public JoinCourseViewModel(ISnackbarService snackbarService, CourseDateService courseDateService, ILearnApiService ilearnApiService)
        {
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _courseDateService = courseDateService ?? throw new ArgumentNullException(nameof(courseDateService));
            _ilearnApiService = ilearnApiService ?? throw new ArgumentNullException(nameof(ilearnApiService));

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await _operationSemaphore.WaitAsync();
            try
            {
                IsLoading = true;
                _currentOperationCts?.Cancel();
                _currentOperationCts = new CancellationTokenSource();
                var token = _currentOperationCts.Token;

                var courses = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    return _courseDateService.GetLocalCourseDatas().ToList();
                }, token);

                if (token.IsCancellationRequested) return;

                lock (_dataLock)
                {
                    _allCoursesCache = courses;
                    _searchCache.Clear();
                    _lastSearchQuery = string.Empty;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        AvailableCourses.Clear();
                        _ = ApplyFiltersAndPaginationAsync();
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    _snackbarService.Show(
                        "加载失败",
                        $"无法加载课程数据：{ex.Message}",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.ErrorCircle16),
                        TimeSpan.FromSeconds(5))
                );
            }
            finally
            {
                IsLoading = false;
                _operationSemaphore.Release();
            }
        }

        private async Task ApplyFiltersAndPaginationAsync()
        {
            if (IsSearching) return;

            await _operationSemaphore.WaitAsync();
            try
            {
                IsSearching = true;
                _currentOperationCts?.Cancel();
                _currentOperationCts = new CancellationTokenSource();
                var token = _currentOperationCts.Token;

                var currentQuery = SearchQuery?.Trim() ?? string.Empty;
                var currentPage = CurrentPage;
                var pageSize = PageSize;

                var (pagedItems, totalCourses, totalPages, paginationInfo) =
                    await Task.Run(() => ProcessDataInBackground(currentQuery, currentPage, pageSize, token), token);

                if (token.IsCancellationRequested) return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        UpdateObservableCollection(PagedCourses, pagedItems);

                        TotalCourses = totalCourses;
                        TotalPages = totalPages;
                        PaginationInfo = paginationInfo;

                        if (CurrentPage > TotalPages && TotalPages > 0)
                        {
                            CurrentPage = TotalPages;
                        }
                        else if (CurrentPage < 1)
                        {
                            CurrentPage = 1;
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    _snackbarService.Show(
                        "处理失败",
                        $"数据处理出错：{ex.Message}",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.ErrorCircle16),
                        TimeSpan.FromSeconds(3))
                );
            }
            finally
            {
                IsSearching = false;
                _operationSemaphore.Release();
            }
        }

        private (List<LocalCourseData> pagedItems, int totalCourses, int totalPages, string paginationInfo)
            ProcessDataInBackground(string searchQuery, int currentPage, int pageSize, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            List<LocalCourseData> filteredItems;

            lock (_dataLock)
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    filteredItems = _allCoursesCache;
                }
                else
                {
                    var cacheKey = searchQuery.ToLowerInvariant();
                    if (!_searchCache.TryGetValue(cacheKey, out filteredItems))
                    {
                        var query = cacheKey;
                        filteredItems = _allCoursesCache.AsParallel()
                            .Where(course =>
                                (course.CourseName?.ToLowerInvariant().Contains(query) == true) ||
                                (course.CourseId?.ToLowerInvariant().Contains(query) == true) ||
                                (course.TeacherName?.ToLowerInvariant().Contains(query) == true) ||
                                (course.Term?.ToLowerInvariant().Contains(query) == true))
                            .ToList();

                        if (_searchCache.Count > 100)
                        {
                            _searchCache.Clear();
                        }
                        _searchCache[cacheKey] = filteredItems;
                    }
                }
            }

            token.ThrowIfCancellationRequested();

            var totalCourses = filteredItems.Count;
            var totalPages = totalCourses == 0 ? 1 : (int)Math.Ceiling((double)totalCourses / pageSize);

            if (currentPage > totalPages && totalPages > 0)
                currentPage = totalPages;
            else if (currentPage < 1)
                currentPage = 1;

            var pagedItems = filteredItems
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            token.ThrowIfCancellationRequested();

            string paginationInfo;
            if (totalCourses == 0)
            {
                paginationInfo = "没有找到课程";
            }
            else
            {
                int startItem = (currentPage - 1) * pageSize + 1;
                int endItem = Math.Min(currentPage * pageSize, totalCourses);
                paginationInfo = $"显示 {startItem}-{endItem} 项，共 {totalCourses:N0} 项";
            }

            return (pagedItems, totalCourses, totalPages, paginationInfo);
        }

        private static void UpdateObservableCollection<T>(ObservableCollection<T> collection, IList<T> newItems)
        {
            if (collection.Count == 0)
            {
                foreach (var item in newItems)
                {
                    collection.Add(item);
                }
                return;
            }

            if (newItems.Count == 0)
            {
                collection.Clear();
                return;
            }

            collection.Clear();
            foreach (var item in newItems)
            {
                collection.Add(item);
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await ApplyFiltersAndPaginationAsync();
        }

        [RelayCommand]
        private async Task FirstPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                await ApplyFiltersAndPaginationAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await ApplyFiltersAndPaginationAsync();
            }
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await ApplyFiltersAndPaginationAsync();
            }
        }

        [RelayCommand]
        private async Task LastPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                await ApplyFiltersAndPaginationAsync();
            }
        }

        [RelayCommand]
        private async Task JoinCourseAsync(LocalCourseData course)
        {
            if (course == null) return;

            SelectedCourse = course;
            var inviteCode = GenerateInviteCode(course);

            try
            {
                await Task.Run(async () => await _ilearnApiService.JoinCourse(inviteCode));

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
        private async Task RefreshCoursesAsync()
        {
            await LoadDataAsync();

            await Application.Current.Dispatcher.InvokeAsync(() =>
                _snackbarService.Show(
                    "刷新完成",
                    "课程列表已更新",
                    ControlAppearance.Info,
                    new SymbolIcon(SymbolRegular.ArrowClockwise16),
                    TimeSpan.FromSeconds(2)
                )
            );
        }

        private static string GenerateInviteCode(LocalCourseData localCourseData)
        {
            return $"{localCourseData.Term}{localCourseData.CourseId}{localCourseData.SectionId}";
        }

        partial void OnSearchQueryChanged(string value)
        {
            lock (_searchLock)
            {
                _searchTimer?.Dispose();
                _searchTimer = new Timer(async _ =>
                {
                    await SearchAsync();
                }, null, TimeSpan.FromMilliseconds(200), Timeout.InfiniteTimeSpan);
            }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentOperationCts?.Cancel();
                _currentOperationCts?.Dispose();
                _searchTimer?.Dispose();
                _operationSemaphore?.Dispose();
            }
        }

        ~JoinCourseViewModel()
        {
            Dispose(false);
        }
    }
}