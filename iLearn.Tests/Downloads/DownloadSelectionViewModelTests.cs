using iLearn.Downloads;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Services;
using iLearn.ViewModels.Pages;
using Xunit;

namespace iLearn.Tests.Downloads;

public sealed class DownloadSelectionViewModelTests
{
    [Fact]
    public void SelectedDownloadText_UpdatesWhenItemsAreSelected()
    {
        var viewModel = CreateViewModel(new[]
        {
            CreateVideo("resource-1", "高等数学", "张老师"),
            CreateVideo("resource-2", "大学英语", "李老师")
        });

        viewModel.Videos[0].IsHdmiSelected = true;
        viewModel.Videos[1].IsTeacherSelected = true;

        Assert.Equal(2, viewModel.SelectedDownloadCount);
        Assert.Equal("已选择 2 个文件", viewModel.SelectedDownloadText);
    }

    [Fact]
    public void IsAllHdmiSelected_SelectsEveryHdmiFile()
    {
        var viewModel = CreateViewModel(new[]
        {
            CreateVideo("resource-1", "高等数学", "张老师"),
            CreateVideo("resource-2", "大学英语", "李老师")
        });

        viewModel.IsAllHdmiSelected = true;

        Assert.All(viewModel.Videos, video => Assert.True(video.IsHdmiSelected));
        Assert.Equal(2, viewModel.SelectedDownloadCount);
    }

    [Fact]
    public void FilteredVideos_MatchesTitleOrTeacher()
    {
        var viewModel = CreateViewModel(new[]
        {
            CreateVideo("resource-1", "高等数学", "张老师"),
            CreateVideo("resource-2", "大学英语", "李老师")
        });

        viewModel.SearchText = "李老师";

        var video = Assert.Single(viewModel.FilteredVideos);
        Assert.Equal("大学英语", video.LiveRecordName);
    }

    [Fact]
    public async Task LoadCourseAsync_LoadsVideosForSelectedCourse()
    {
        var api = new FakeILearnApiService();
        var viewModel = CreateViewModel(Array.Empty<LiveAndRecordInfo>(), api: api);

        await viewModel.LoadCourseAsync(new ClassInfo
        {
            TermId = "2026-spring",
            ClassId = "class-1",
            CourseName = "高等数学"
        });

        Assert.Equal(1, api.LiveRecordCalls);
        Assert.Equal("2026-spring", api.LastTermId);
        Assert.Equal("class-1", api.LastClassId);
        Assert.Equal("高等数学 第 1 讲", viewModel.Videos[0].LiveRecordName);
        Assert.Equal("共 2 个视频", viewModel.VideoStatusText);
    }

    [Fact]
    public async Task DownloadSelectedCommand_ShowsTipWhenNothingSelected()
    {
        var notifications = new NotificationService();
        var viewModel = CreateViewModel(new[]
        {
            CreateVideo("resource-1", "高等数学", "张老师")
        }, notifications);

        await viewModel.DownloadSelectedCommand.ExecuteAsync(null);

        var notification = Assert.Single(notifications.Items);
        Assert.Equal(AppNotificationKind.Info, notification.Kind);
        Assert.Equal("请选择要下载的视频", notification.Title);
    }

    [Fact]
    public async Task DownloadSelectedCommand_QueuesBothPerspectivesWithOneSubtitle()
    {
        var downloadDirectory = Path.Combine(Path.GetTempPath(), $"ilearn-test-{Guid.NewGuid():N}");
        var queue = new DownloadQueueService(new FakeDownloadEngine());
        var viewModel = CreateViewModel(
            new[] { CreateVideo("resource-1", "高等数学", "张老师") },
            queue: queue,
            appConfig: new AppConfig { DownloadPath = downloadDirectory });

        try
        {
            viewModel.Videos[0].IsHdmiSelected = true;
            viewModel.Videos[0].IsTeacherSelected = true;

            await viewModel.DownloadSelectedCommand.ExecuteAsync(null);

            var tasks = await ReadStableSnapshotAsync(queue, expectedCount: 3);
            Assert.Contains(tasks, task => task.Id == "resource-1-HDMI");
            Assert.Contains(tasks, task => task.Id == "resource-1-教师");
            Assert.Contains(tasks, task => task.Id == "resource-1-subtitle");
            Assert.Equal("未选择", viewModel.SelectedDownloadText);
        }
        finally
        {
            if (Directory.Exists(downloadDirectory))
                Directory.Delete(downloadDirectory, recursive: true);
        }
    }

    private static async Task<List<DownloadTaskSnapshot>> ReadStableSnapshotAsync(
        DownloadQueueService queue,
        int expectedCount)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            try
            {
                var tasks = queue.Tasks.ToList();
                if (tasks.Count >= expectedCount && tasks.All(task => task.Status == DownloadTaskStatus.Completed))
                    return tasks;
            }
            catch (InvalidOperationException)
            {
                // The queue publishes UI snapshots asynchronously; retry until updates settle.
            }

            await Task.Delay(10, timeout.Token);
        }
    }

    private static VideoDownloadListViewModel CreateViewModel(
        IEnumerable<LiveAndRecordInfo> videos,
        NotificationService? notifications = null,
        DownloadQueueService? queue = null,
        FakeILearnApiService? api = null,
        AppConfig? appConfig = null)
    {
        return new VideoDownloadListViewModel(
            videos.ToList(),
            queue ?? new DownloadQueueService(new FakeDownloadEngine()),
            api ?? new FakeILearnApiService(),
            notifications ?? new NotificationService(),
            appConfig ?? new AppConfig());
    }

    private static LiveAndRecordInfo CreateVideo(string resourceId, string title, string teacherName)
    {
        return new LiveAndRecordInfo
        {
            ResourceId = resourceId,
            LiveRecordName = title,
            TeacherName = teacherName
        };
    }

    private sealed class FakeILearnApiService : ILearnApiService
    {
        public int LiveRecordCalls { get; private set; }
        public string? LastTermId { get; private set; }
        public string? LastClassId { get; private set; }

        public override Task<List<LiveAndRecordInfo>> GetLiveAndRecordInfoAsync(string termId, string classId)
        {
            LiveRecordCalls++;
            LastTermId = termId;
            LastClassId = classId;
            return Task.FromResult(new List<LiveAndRecordInfo>
            {
                CreateVideo("resource-1", "高等数学 第 1 讲", "张老师"),
                CreateVideo("resource-2", "高等数学 第 2 讲", "张老师")
            });
        }

        public override Task<VideoInfo> GetVideoInfoAsync(string resourceId)
        {
            return Task.FromResult(new VideoInfo
            {
                LiveRecordId = resourceId,
                ResourceName = "测试视频",
                PhaseUrl = "https://example.test/subtitle.srt",
                VideoList = new List<Video>
                {
                    new() { VideoPath = "https://example.test/teacher.mp4" },
                    new() { VideoPath = "https://example.test/hdmi.mp4" }
                }
            });
        }
    }

    private sealed class FakeDownloadEngine : IDownloadEngine
    {
        public Task DownloadAsync(
            DownloadRequest request,
            string outputPath,
            IProgress<DownloadTaskSnapshot> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(DownloadTaskSnapshot.Downloading(request, 1, 1, 0, null));
            return Task.CompletedTask;
        }
    }
}
