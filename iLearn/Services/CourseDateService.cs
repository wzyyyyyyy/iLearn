using iLearn.Models;
using LiteDB;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Caching;
using System.Text;

namespace iLearn.Services
{
    public class CourseDateService : IDisposable
    {
        private static readonly object _lockObject = new object();
        private static LiteDatabase? _sharedDb;
        private static string? _dbPath;
        private static bool _isInitialized = false;

        // 分页缓存机制 - 缓存前3页和后3页
        private static readonly ConcurrentDictionary<string, List<LocalCourseData>> _pageCache = new();
        private static readonly ConcurrentDictionary<string, DateTime> _pageCacheTimestamps = new();

        // 搜索结果缓存
        private static readonly MemoryCache _searchCache = new("SearchCache");

        // 全量数据缓存（用于计算总数等）
        private static List<LocalCourseData>? _allDataCache;
        private static DateTime _allDataCacheTimestamp = DateTime.MinValue;

        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
        private static readonly int _defaultPageSize = 50;
        private static readonly int _pagesToCache = 3; // 缓存前后3页

        private static readonly ConcurrentDictionary<string, List<string>> _indexCache = new();

        public CourseDateService()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            if (_isInitialized && _sharedDb != null)
                return;

            lock (_lockObject)
            {
                if (_isInitialized && _sharedDb != null)
                    return;

                try
                {
                    _dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "iLearn", "CourseDate.db");

                    if (!File.Exists(_dbPath))
                    {
                        CopyDatabaseFromResources(_dbPath);
                    }

                    // 尝试以只读模式打开数据库
                    var connectionString = new ConnectionString
                    {
                        Filename = _dbPath,
                        Connection = ConnectionType.Shared,
                        ReadOnly = true
                    };

                    _sharedDb = new LiteDatabase(connectionString);

                    // 由于是只读模式，不能创建索引，索引应该在数据库创建时就存在
                    try
                    {
                        var col = _sharedDb.GetCollection<LocalCourseData>("courses");
                        // 测试数据库连接
                        _ = col.Count();
                    }
                    catch (Exception indexEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"数据库连接测试警告: {indexEx.Message}");
                        // 继续执行，不影响基本功能
                    }

                    _isInitialized = true;

                    // 预热缓存 - 异步加载前几页数据
                    _ = Task.Run(PrewarmCache);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");

                    // 尝试备用方案：以非共享模式打开
                    try
                    {
                        _sharedDb?.Dispose();
                        var fallbackConnectionString = new ConnectionString
                        {
                            Filename = _dbPath,
                            Connection = ConnectionType.Direct,
                            ReadOnly = true
                        };

                        _sharedDb = new LiteDatabase(fallbackConnectionString);
                        _isInitialized = true;

                        System.Diagnostics.Debug.WriteLine("使用备用连接模式初始化成功");
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"备用初始化也失败: {fallbackEx.Message}");
                        throw new InvalidOperationException($"数据库初始化失败: {ex.Message}", ex);
                    }
                }
            }
        }

        private static void CopyDatabaseFromResources(string localPath)
        {
            var uri = new Uri("pack://application:,,,/Assets/CourseDate.db");
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

            using var resourceStream = (Application.GetResourceStream(uri)?.Stream) ?? throw new FileNotFoundException("数据库资源文件未找到");
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
            resourceStream.CopyTo(fileStream);

            // 复制完成后，为新数据库创建必要的索引
            CreateDatabaseIndexes(localPath);
        }

        // 为数据库创建索引（仅在数据库创建时调用一次）
        private static void CreateDatabaseIndexes(string dbPath)
        {
            try
            {
                using var db = new LiteDatabase(new ConnectionString
                {
                    Filename = dbPath,
                    ReadOnly = false // 临时设为可写以创建索引
                });

                var col = db.GetCollection<LocalCourseData>("courses");

                // 创建索引以提升查询性能
                col.EnsureIndex(x => x.CourseId);
                col.EnsureIndex(x => x.CourseName);
                col.EnsureIndex(x => x.TeacherName);
                col.EnsureIndex(x => x.Term);
            }
            catch (Exception ex)
            {
                throw new Exception($"创建数据库索引失败: {ex.Message}");
            }
        }

        // 预热缓存
        private static async Task PrewarmCache()
        {
            try
            {
                // 异步加载前3页数据
                for (int page = 0; page < _pagesToCache; page++)
                {
                    int skip = page * _defaultPageSize;
                    var data = await Task.Run(() => GetPageFromDatabase(skip, _defaultPageSize));

                    // 缓存预热的数据
                    string cacheKey = $"page_{page}_{_defaultPageSize}";
                    CachePageData(cacheKey, data);

                    await Task.Delay(50); // 避免阻塞主线程
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"预热缓存失败: {ex.Message}");
            }
        }

        public List<LocalCourseData> GetLocalCourseDatas()
        {
            return GetLocalCourseDatasWithPaging(0, int.MaxValue);
        }

        public async Task<List<LocalCourseData>> GetLocalCourseDatasAsync()
        {
            return await Task.Run(() => GetLocalCourseDatas());
        }

        // 优化的分页查询方法
        public List<LocalCourseData> GetLocalCourseDatas(int skip, int limit)
        {
            return GetLocalCourseDatasWithPaging(skip, limit);
        }

        private List<LocalCourseData> GetLocalCourseDatasWithPaging(int skip, int limit)
        {
            if (_sharedDb is null)
            {
                throw new InvalidOperationException("数据库未初始化");
            }

            if (limit == int.MaxValue)
            {
                return GetAllDataWithCache();
            }

            int pageIndex = skip / _defaultPageSize;
            string cacheKey = $"page_{pageIndex}_{limit}";

            if (TryGetPageFromCache(cacheKey, out var cachedPage))
            {
                return cachedPage.Skip(skip % _defaultPageSize).Take(limit).ToList();
            }

            var data = GetPageFromDatabase(skip, limit);

            if (limit == _defaultPageSize)
            {
                CachePageData(cacheKey, data);

                _ = Task.Run(() => PreloadAdjacentPages(pageIndex));
            }

            return data;
        }

        private List<LocalCourseData> GetAllDataWithCache()
        {
            if (_allDataCache != null &&
                DateTime.Now - _allDataCacheTimestamp < _cacheExpiration)
            {
                return [];
            }

            var col = _sharedDb!.GetCollection<LocalCourseData>("courses");
            _allDataCache = col.FindAll().ToList();
            _allDataCacheTimestamp = DateTime.Now;

            return [];
        }

        private static bool TryGetPageFromCache(string cacheKey, out List<LocalCourseData> data)
        {
            if (!_pageCache.TryGetValue(cacheKey, out data) ||
                !_pageCacheTimestamps.TryGetValue(cacheKey, out var timestamp))
            {
                data = [];
                return false;
            }

            if (DateTime.Now - timestamp >= _cacheExpiration)
            {
                data = [];
                return false;
            }

            return true;
        }

        private static List<LocalCourseData> GetPageFromDatabase(int skip, int limit)
        {
            var col = _sharedDb!.GetCollection<LocalCourseData>("courses");
            return col.FindAll().Skip(skip).Take(limit).ToList();
        }

        private static void CachePageData(string cacheKey, List<LocalCourseData> data)
        {
            _pageCache.AddOrUpdate(cacheKey, data, (key, oldValue) => data);
            _pageCacheTimestamps.AddOrUpdate(cacheKey, DateTime.Now, (key, oldValue) => DateTime.Now);
        }

        private static async Task PreloadAdjacentPages(int currentPageIndex)
        {
            try
            {
                if (_sharedDb is null) return;

                var col = _sharedDb.GetCollection<LocalCourseData>("courses");
                var totalCount = col.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / _defaultPageSize);

                // 预加载前后3页
                for (int offset = 1; offset <= _pagesToCache; offset++)
                {
                    if (currentPageIndex + offset < totalPages)
                    {
                        int nextSkip = (currentPageIndex + offset) * _defaultPageSize;
                        string nextKey = $"page_{currentPageIndex + offset}_{_defaultPageSize}";

                        if (!TryGetPageFromCache(nextKey, out _))
                        {
                            var nextData = await Task.Run(() => GetPageFromDatabase(nextSkip, _defaultPageSize));
                            CachePageData(nextKey, nextData);
                        }
                    }

                    if (currentPageIndex - offset >= 0)
                    {
                        int prevSkip = (currentPageIndex - offset) * _defaultPageSize;
                        string prevKey = $"page_{currentPageIndex - offset}_{_defaultPageSize}";

                        if (!TryGetPageFromCache(prevKey, out _))
                        {
                            var prevData = await Task.Run(() => GetPageFromDatabase(prevSkip, _defaultPageSize));
                            CachePageData(prevKey, prevData);
                        }
                    }

                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"预加载页面失败: {ex.Message}");
            }
        }

        public List<LocalCourseData> GetLocalCourseDatas(Func<LocalCourseData, bool> predicate)
        {
            if (_sharedDb is null)
            {
                throw new InvalidOperationException("数据库未初始化");
            }

            string predicateKey = predicate.Method.ToString();
            string cacheKey = $"search_{predicateKey.GetHashCode()}";

            if (_searchCache.Get(cacheKey) is List<LocalCourseData> cachedResult)
            {
                return [.. cachedResult];
            }

            var col = _sharedDb.GetCollection<LocalCourseData>("courses");
            var result = col.FindAll().Where(predicate).ToList();

            _searchCache.Set(cacheKey, result, DateTimeOffset.Now.AddMinutes(2));

            return result;
        }

        // 优化的文本搜索方法
        public List<LocalCourseData> SearchCourses(string searchText, int skip = 0, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return GetLocalCourseDatas(skip, limit);
            }

            string cacheKey = $"textsearch_{searchText.ToLower().GetHashCode()}_{skip}_{limit}";

            if (_searchCache.Get(cacheKey) is List<LocalCourseData> cachedResult)
            {
                return cachedResult;
            }

            if (_sharedDb is null)
            {
                throw new InvalidOperationException("数据库未初始化");
            }

            var col = _sharedDb.GetCollection<LocalCourseData>("courses");
            var searchLower = searchText.ToLower();

            var result = new List<LocalCourseData>();

            try
            {
                // 按课程ID搜索
                result = col.Find(x => x.CourseId.Contains(searchText)).Skip(skip).Take(limit).ToList();

                if (!result.Any())
                {
                    // 按课程名称搜索
                    result = col.Find(x => x.CourseName.Contains(searchText)).Skip(skip).Take(limit).ToList();
                }

                if (!result.Any())
                {
                    // 按教师名称搜索
                    result = col.Find(x => x.TeacherName.Contains(searchText)).Skip(skip).Take(limit).ToList();
                }

                if (!result.Any())
                {
                    // 全文搜索（性能较低）
                    result = col.FindAll()
                               .Where(x => ContainsSearchText(x, searchLower))
                               .Skip(skip)
                               .Take(limit)
                               .ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"搜索过程出错: {ex.Message}");
                // 回退到基本搜索
                result = col.FindAll()
                           .Where(x => ContainsSearchText(x, searchLower))
                           .Skip(skip)
                           .Take(limit)
                           .ToList();
            }

            _searchCache.Set(cacheKey, result, DateTimeOffset.Now.AddMinutes(3));
            return result;
        }

        private static bool ContainsSearchText(LocalCourseData course, string searchText)
        {
            return course.CourseId.ToString().ToLower().Contains(searchText)
                || (course.CourseName?.ToLower().Contains(searchText) ?? false)
                || (course.TeacherName?.ToLower().Contains(searchText) ?? false)
                || (course.Term?.ToLower().Contains(searchText) ?? false);
        }

        public int GetCoursesCount()
        {
            if (_allDataCache != null &&
                DateTime.Now - _allDataCacheTimestamp < _cacheExpiration)
            {
                return _allDataCache.Count;
            }

            if (_sharedDb is null)
            {
                throw new InvalidOperationException("数据库未初始化");
            }

            var col = _sharedDb.GetCollection<LocalCourseData>("courses");
            return col.Count();
        }

        // 获取搜索结果总数（用于分页）
        public int GetSearchResultsCount(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return GetCoursesCount();
            }

            string cacheKey = $"searchcount_{searchText.ToLower().GetHashCode()}";

            if (_searchCache.Get(cacheKey) is int cachedCount)
            {
                return cachedCount;
            }

            if (_sharedDb is null)
            {
                throw new InvalidOperationException("数据库未初始化");
            }

            var col = _sharedDb.GetCollection<LocalCourseData>("courses");
            var searchLower = searchText.ToLower();

            int count = col.Count(x => x.CourseId.Contains(searchLower));

            _searchCache.Set(cacheKey, count, DateTimeOffset.Now.AddMinutes(5));
            return count;
        }

        // 清理过期缓存
        private static void CleanExpiredCache()
        {
            var expiredKeys = _pageCacheTimestamps
                .Where(kvp => DateTime.Now - kvp.Value > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _pageCache.TryRemove(key, out _);
                _pageCacheTimestamps.TryRemove(key, out _);
            }
        }

        public static void ClearCache()
        {
            _pageCache.Clear();
            _pageCacheTimestamps.Clear();
            _allDataCache = null;
            _allDataCacheTimestamp = DateTime.MinValue;
            _indexCache.Clear();

            // 创建新的搜索缓存实例
            try
            {
                var oldCache = _searchCache;
                oldCache?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理搜索缓存时出错: {ex.Message}");
            }
        }

        public void Dispose()
        {
            CleanExpiredCache();
        }

        public static void DisposeSharedResources()
        {
            lock (_lockObject)
            {
                try
                {
                    _sharedDb?.Dispose();
                    _sharedDb = null;
                    _pageCache.Clear();
                    _pageCacheTimestamps.Clear();
                    _searchCache?.Dispose();
                    _allDataCache = null;
                    _indexCache.Clear();
                    _isInitialized = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放资源时出错: {ex.Message}");
                }
            }
        }

        public static CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                PageCacheCount = _pageCache.Count,
                SearchCacheCount = _searchCache?.GetCount() ?? 0,
                AllDataCached = _allDataCache != null,
                LastAllDataUpdate = _allDataCacheTimestamp
            };
        }
    }

    public class CacheStats
    {
        public int PageCacheCount { get; set; }
        public long SearchCacheCount { get; set; }
        public bool AllDataCached { get; set; }
        public DateTime LastAllDataUpdate { get; set; }
    }
}