# Video Download And Subtitle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make video downloads easier to start, clearer to monitor, safer to control, and make local playback automatically load the matching subtitle file.

**Architecture:** Keep the current WPF + MVVM + `Downloader` package architecture. Add a small download queue contract around `VideoDownloadService`, keep UI state in view models, and solve subtitle auto-loading by matching the local video file to downloaded `.vtt`/`.srt` files before generating the temporary `VideoPlayer.html`.

**Tech Stack:** .NET 10 WPF, CommunityToolkit.Mvvm, WPF-UI, Downloader 4.0.3, existing embedded `Assets/VideoPlayer.html`, xUnit test project converted from `iLearn.Tests`.

---

## File Structure

- Modify `iLearn/Models/DownloadItem.cs`: add user-facing status text, formatted size/progress helpers, selected perspective metadata, and estimated remaining time.
- Create `iLearn/Services/DownloadStatus.cs`: centralize download status constants to remove fragile string comparisons.
- Create `iLearn/Services/FileNameService.cs`: sanitize names and build consistent video/subtitle filenames.
- Modify `iLearn/Services/VideoDownloadService.cs`: validate paths, avoid duplicate queued items, expose snapshot refresh, make cancel/retry safe, and report status using constants.
- Modify `iLearn/ViewModels/Pages/VideoDownloadListViewModel.cs`: replace fire-and-forget selection handling with a preparation pipeline that validates video streams, queues visible tasks, reports partial failures, and clears selections after successful queueing.
- Modify `iLearn/Views/Pages/VideoDownloadListPage.xaml`: tighten the video selection list layout, add selected-count feedback, and make primary controls visually consistent.
- Modify `iLearn/ViewModels/Pages/DownloadManageViewModel.cs`: compute total speed, counts, ETA, and command visibility from status constants; remove stale completed items only when explicitly requested.
- Modify `iLearn/Views/Pages/DownloadManagePage.xaml`: redesign the download manager as a compact operational list with stable columns, readable speed/size/ETA, and icon buttons.
- Modify `iLearn/Models/LocalVideoFile.cs`: add nullable partner return, robust parsing for current downloaded names, and subtitle candidate discovery.
- Modify `iLearn/ViewModels/Pages/LocalVideoViewModel.cs`: inject the auto-detected subtitle path into `VideoPlayer.html` and tell the user whether a subtitle was found.
- Modify `iLearn/Assets/VideoPlayer.html`: accept a local subtitle URL from `_SUBTITLE_`, decode local `file:///` URLs, and keep manual upload as a fallback.
- Modify `iLearn.Tests/iLearn.Tests.csproj`: convert the diagnostics project into a runnable xUnit test project.
- Replace `iLearn.Tests/Program.cs` with focused tests for filename generation, local video parsing, subtitle matching, and download item formatting.

## Task 1: Test Harness Conversion

**Files:**
- Modify: `iLearn.Tests/iLearn.Tests.csproj`
- Delete content and replace: `iLearn.Tests/Program.cs`
- Create: `iLearn.Tests/DownloadNamingTests.cs`

- [x] **Step 1: Convert the test project to xUnit**

Replace `iLearn.Tests/iLearn.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="coverlet.collector" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\iLearn\iLearn.csproj" />
  </ItemGroup>
</Project>
```

- [x] **Step 2: Remove the interactive diagnostics entry point**

Replace `iLearn.Tests/Program.cs` with:

```csharp
namespace iLearn.Tests;

public sealed class ProgramPlaceholder
{
    [Xunit.Fact]
    public void Placeholder()
    {
        Assert.True(true);
    }
}
```

- [x] **Step 3: Verify the harness runs**

Run: `dotnet test iLearn.sln --configuration Debug -v minimal`

Expected: the test host discovers one placeholder test and exits with `Passed`.

- [x] **Step 4: Commit**

```bash
git add iLearn.Tests/iLearn.Tests.csproj iLearn.Tests/Program.cs
git commit -m "test: convert diagnostics project to xunit"
```

## Task 2: Filename And Subtitle Matching Unit Tests

**Files:**
- Create: `iLearn/Services/FileNameService.cs`
- Modify: `iLearn/Models/LocalVideoFile.cs`
- Create: `iLearn.Tests/FileNameServiceTests.cs`
- Create: `iLearn.Tests/LocalVideoFileTests.cs`

- [x] **Step 1: Add failing filename tests**

Create `iLearn.Tests/FileNameServiceTests.cs`:

```csharp
using iLearn.Services;

namespace iLearn.Tests;

public sealed class FileNameServiceTests
{
    [Fact]
    public void BuildVideoFileName_ReplacesInvalidCharacters_AndAddsPerspective()
    {
        var fileName = FileNameService.BuildVideoFileName("高数/第一讲:导论", "HDMI");

        Assert.Equal("高数_第一讲_导论_HDMI.mp4", fileName);
    }

    [Fact]
    public void BuildSubtitleFileName_UsesResourceName()
    {
        var fileName = FileNameService.BuildSubtitleFileName("高数/第一讲:导论");

        Assert.Equal("高数_第一讲_导论.vtt", fileName);
    }
}
```

- [x] **Step 2: Run the new tests and confirm failure**

Run: `dotnet test iLearn.Tests/iLearn.Tests.csproj --configuration Debug --filter FileNameServiceTests -v minimal`

Expected: compile fails because `FileNameService` does not exist.

- [x] **Step 3: Implement filename service**

Create `iLearn/Services/FileNameService.cs`:

```csharp
using System.IO;

namespace iLearn.Services;

public static class FileNameService
{
    public static string BuildVideoFileName(string sourceName, string perspective)
    {
        var safeName = SanitizeFileName(sourceName);
        var safePerspective = SanitizeFileName(perspective);
        return $"{safeName}_{safePerspective}.mp4";
    }

    public static string BuildSubtitleFileName(string sourceName)
    {
        return $"{SanitizeFileName(sourceName)}.vtt";
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "未命名";

        var result = name.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            result = result.Replace(invalid, '_');

        return result;
    }
}
```

- [x] **Step 4: Add failing local video subtitle tests**

Create `iLearn.Tests/LocalVideoFileTests.cs`:

```csharp
using iLearn.Models;

namespace iLearn.Tests;

public sealed class LocalVideoFileTests
{
    [Fact]
    public void FindSubtitlePath_ReturnsSubtitleWithSameBaseName()
    {
        var root = CreateTempRoot();
        var videoPath = Path.Combine(root, "高数_第一讲_HDMI.mp4");
        var subtitlePath = Path.Combine(root, "Subtitles", "高数_第一讲.vtt");
        Directory.CreateDirectory(Path.GetDirectoryName(subtitlePath)!);
        File.WriteAllText(videoPath, "");
        File.WriteAllText(subtitlePath, "WEBVTT");

        var video = LocalVideoFile.FromFileName(videoPath);

        Assert.Equal(subtitlePath, video.FindSubtitlePath(root));
    }

    [Fact]
    public void GetPartnerVideo_ReturnsNullWhenPairIsMissing()
    {
        var root = CreateTempRoot();
        var videoPath = Path.Combine(root, "高数_第一讲_HDMI.mp4");
        File.WriteAllText(videoPath, "");

        var video = LocalVideoFile.FromFileName(videoPath);

        Assert.Null(video.GetPartnerVideo());
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ilearn-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
```

- [x] **Step 5: Run the local video tests and confirm failure**

Run: `dotnet test iLearn.Tests/iLearn.Tests.csproj --configuration Debug --filter LocalVideoFileTests -v minimal`

Expected: compile fails because `FindSubtitlePath` does not exist and `GetPartnerVideo` is non-nullable.

- [x] **Step 6: Implement local subtitle discovery**

Modify `iLearn/Models/LocalVideoFile.cs`:

```csharp
public LocalVideoFile? GetPartnerVideo()
{
    var path = Perspective switch
    {
        "HDMI" => FullPath.Replace("_HDMI.mp4", "_教师.mp4"),
        "教师" => FullPath.Replace("_教师.mp4", "_HDMI.mp4"),
        _ => string.Empty
    };

    return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
        ? FromFileName(path)
        : null;
}

public string? FindSubtitlePath(string downloadRoot)
{
    var videoBaseName = Path.GetFileNameWithoutExtension(FullPath);
    var baseWithoutPerspective = videoBaseName
        .Replace("_HDMI", "", StringComparison.OrdinalIgnoreCase)
        .Replace("_教师", "", StringComparison.OrdinalIgnoreCase)
        .Replace("_teacher", "", StringComparison.OrdinalIgnoreCase);

    var candidates = new[]
    {
        Path.Combine(Path.GetDirectoryName(FullPath) ?? downloadRoot, $"{baseWithoutPerspective}.vtt"),
        Path.Combine(Path.GetDirectoryName(FullPath) ?? downloadRoot, $"{baseWithoutPerspective}.srt"),
        Path.Combine(downloadRoot, "Subtitles", $"{baseWithoutPerspective}.vtt"),
        Path.Combine(downloadRoot, "Subtitles", $"{baseWithoutPerspective}.srt")
    };

    return candidates.FirstOrDefault(File.Exists);
}
```

Also update the method signature wherever needed from `LocalVideoFile GetPartnerVideo()` to `LocalVideoFile? GetPartnerVideo()`.

- [x] **Step 7: Run tests**

Run: `dotnet test iLearn.Tests/iLearn.Tests.csproj --configuration Debug --filter "FileNameServiceTests|LocalVideoFileTests" -v minimal`

Expected: all new tests pass.

- [x] **Step 8: Commit**

```bash
git add iLearn/Services/FileNameService.cs iLearn/Models/LocalVideoFile.cs iLearn.Tests/FileNameServiceTests.cs iLearn.Tests/LocalVideoFileTests.cs
git commit -m "feat: add download filename and subtitle matching"
```

## Task 3: Download Queue Reliability

**Files:**
- Create: `iLearn/Services/DownloadStatus.cs`
- Modify: `iLearn/Models/DownloadItem.cs`
- Modify: `iLearn/Services/VideoDownloadService.cs`

- [x] **Step 1: Add status constants**

Create `iLearn/Services/DownloadStatus.cs`:

```csharp
namespace iLearn.Services;

public static class DownloadStatus
{
    public const string Waiting = "Waiting";
    public const string Preparing = "Preparing";
    public const string Queued = "Queued";
    public const string Downloading = "Downloading";
    public const string Paused = "Paused";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}
```

- [x] **Step 2: Add download item helpers**

Modify `iLearn/Models/DownloadItem.cs`:

```csharp
namespace iLearn.Models;

public partial class DownloadItem : ObservableObject
{
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string fileName = string.Empty;
    [ObservableProperty] private string outputPath = string.Empty;
    [ObservableProperty] private double progress;
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private string speed = "0 KB/s";
    [ObservableProperty] private double speedValue;
    [ObservableProperty] private long bytesReceived;
    [ObservableProperty] private long totalBytes;
    [ObservableProperty] private string perspective = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public string SizeText => TotalBytes > 0
        ? $"{FormatBytes(BytesReceived)} / {FormatBytes(TotalBytes)}"
        : FormatBytes(BytesReceived);

    public string RemainingText
    {
        get
        {
            if (SpeedValue <= 0 || TotalBytes <= 0 || BytesReceived <= 0)
                return "--";

            var seconds = Math.Max(0, (TotalBytes - BytesReceived) / SpeedValue);
            var time = TimeSpan.FromSeconds(seconds);
            return time.TotalHours >= 1 ? $"{time:hh\\:mm\\:ss}" : $"{time:mm\\:ss}";
        }
    }

    partial void OnBytesReceivedChanged(long value)
    {
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(RemainingText));
    }

    partial void OnTotalBytesChanged(long value)
    {
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(RemainingText));
    }

    partial void OnSpeedValueChanged(double value)
    {
        OnPropertyChanged(nameof(RemainingText));
    }

    private static string FormatBytes(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
```

- [x] **Step 3: Harden `StartDownloadAsync`**

Modify `VideoDownloadService.StartDownloadAsync` so it starts with this implementation:

```csharp
public async Task<bool> StartDownloadAsync(string url, string fileName, string outputPath, string perspective = "")
{
    if (_disposed)
        throw new ObjectDisposedException(nameof(VideoDownloadService));

    if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(outputPath))
        return false;

    Directory.CreateDirectory(outputPath);

    if (_activeDownloads.ContainsKey(url))
        return false;

    string fullPath = Path.Combine(outputPath, fileName);

    var item = new DownloadItem
    {
        Url = url,
        FileName = fileName,
        OutputPath = fullPath,
        Perspective = perspective,
        Status = DownloadStatus.Waiting,
        Speed = "0 KB/s",
        SpeedValue = 0
    };

    _activeDownloads[url] = item;
    _downloadQueue.Enqueue(new DownloadRequest(url, fileName, outputPath));

    _ = Task.Run(() => ProcessDownloadQueueAsync(_cancellationTokenSource.Token));
    await Task.CompletedTask;
    return true;
}
```

- [x] **Step 4: Replace status string literals inside `VideoDownloadService`**

Change every assignment and comparison inside `VideoDownloadService`:

```csharp
item.Status = DownloadStatus.Queued;
item.Status = DownloadStatus.Downloading;
item.Status = DownloadStatus.Cancelled;
item.Status = DownloadStatus.Failed;
item.Status = DownloadStatus.Completed;
item.Status = DownloadStatus.Paused;
item.Status = DownloadStatus.Waiting;
```

Change the completed removal predicate to:

```csharp
.Where(kvp => kvp.Value.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
```

- [x] **Step 5: Make cancel file deletion safe**

Replace the current `Task.Delay(1000).ContinueWith((_)=>File.Delete(item.OutputPath));` block with:

```csharp
if (_activeDownloads.TryGetValue(url, out var item))
{
    item.Status = DownloadStatus.Cancelled;
    ResetItemSpeed(item);

    _ = Task.Run(async () =>
    {
        await Task.Delay(1000);
        if (File.Exists(item.OutputPath))
            File.Delete(item.OutputPath);
    });
}
```

- [x] **Step 6: Run build**

Run: `dotnet build iLearn.sln --configuration Debug -v minimal`

Expected: build succeeds.

- [x] **Step 7: Commit**

```bash
git add iLearn/Services/DownloadStatus.cs iLearn/Models/DownloadItem.cs iLearn/Services/VideoDownloadService.cs
git commit -m "fix: harden video download queue"
```

## Task 4: Easier Download Selection Flow

**Files:**
- Modify: `iLearn/ViewModels/Pages/VideoDownloadListViewModel.cs`
- Modify: `iLearn/Views/Pages/VideoDownloadListPage.xaml`

- [x] **Step 1: Add selected-count state**

Add these properties to `VideoDownloadListViewModel`:

```csharp
[ObservableProperty]
private int _selectedDownloadCount;

[ObservableProperty]
private bool _isPreparingDownloads;

public string SelectedDownloadText => SelectedDownloadCount == 0
    ? "未选择"
    : $"已选择 {SelectedDownloadCount} 个文件";
```

Add this helper:

```csharp
private void RefreshSelectedDownloadCount()
{
    SelectedDownloadCount = Videos.Count(v => v.IsHdmiSelected) + Videos.Count(v => v.IsTeacherSelected);
    OnPropertyChanged(nameof(SelectedDownloadText));
}
```

Call `RefreshSelectedDownloadCount()` after all-select changes and from `OnVideoPropertyChanged`.

- [x] **Step 2: Make the download command await preparation**

Replace `DownloadSelected` with:

```csharp
[RelayCommand]
private async Task DownloadSelected()
{
    var selections = Videos
        .Where(v => !string.IsNullOrWhiteSpace(v.ResourceId))
        .SelectMany(v => new[]
        {
            new { Video = v, Perspective = "HDMI", Selected = v.IsHdmiSelected },
            new { Video = v, Perspective = "教师", Selected = v.IsTeacherSelected }
        })
        .Where(x => x.Selected)
        .ToList();

    if (selections.Count == 0)
    {
        ShowSnackbar("请选择要下载的视频", "没有选中任何视频进行下载", ControlAppearance.Info);
        return;
    }

    IsPreparingDownloads = true;
    var queued = 0;
    var failed = 0;

    try
    {
        foreach (var selection in selections)
        {
            if (await DownloadVideoAsync(selection.Video, selection.Perspective))
                queued++;
            else
                failed++;
        }
    }
    finally
    {
        IsPreparingDownloads = false;
    }

    RefreshSelectedDownloadCount();
    ShowSnackbar("下载任务已添加", $"成功加入队列 {queued} 个，失败 {failed} 个", failed == 0 ? ControlAppearance.Success : ControlAppearance.Caution);
}
```

- [x] **Step 3: Return success from `DownloadVideoAsync`**

Change the method signature and body to:

```csharp
private async Task<bool> DownloadVideoAsync(LiveAndRecordInfo video, string perspective)
{
    try
    {
        var folder = _appConfig.DownloadPath;
        Directory.CreateDirectory(folder);

        var videoInfo = await _iLearnApiService.GetVideoInfoAsync(video.ResourceId);
        await DownloadSubtitleAsync(videoInfo);

        var videoSource = perspective == "HDMI"
            ? videoInfo.VideoList.ElementAtOrDefault(1)
            : videoInfo.VideoList.ElementAtOrDefault(0);

        if (string.IsNullOrWhiteSpace(videoSource?.VideoPath))
            return false;

        var fileName = FileNameService.BuildVideoFileName(video.LiveRecordName, perspective);
        return await _downloadService.StartDownloadAsync(videoSource.VideoPath, fileName, folder, perspective);
    }
    catch (Exception ex)
    {
        ShowSnackbar("下载失败", $"无法下载视频 {video.LiveRecordName}: {ex.Message}", ControlAppearance.Danger);
        return false;
    }
}
```

- [x] **Step 4: Await subtitle queueing**

Replace `DownloadSubtitle` with:

```csharp
private async Task DownloadSubtitleAsync(VideoInfo videoInfo)
{
    if (string.IsNullOrWhiteSpace(videoInfo.PhaseUrl))
        return;

    var fileName = FileNameService.BuildSubtitleFileName(videoInfo.ResourceName);
    var folder = Path.Combine(_appConfig.DownloadPath, "Subtitles");
    await _downloadService.StartDownloadAsync(videoInfo.PhaseUrl, fileName, folder, "字幕");
}
```

- [x] **Step 5: Update top controls in XAML**

In `VideoDownloadListPage.xaml`, add a selected-count text before the download button:

```xml
<TextBlock Text="{Binding SelectedDownloadText}"
           VerticalAlignment="Center"
           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
           Margin="0,0,12,0"/>
<ui:Button Content="添加到下载"
           Icon="Download24"
           Command="{Binding DownloadSelectedCommand}"
           Appearance="Primary"
           IsEnabled="{Binding IsPreparingDownloads, Converter={StaticResource InverseBooleanConverter}}"
           Margin="0,0,5,0"/>
```

- [x] **Step 6: Run build**

Run: `dotnet build iLearn.sln --configuration Debug -v minimal`

Expected: build succeeds and no new nullable errors appear.

- [x] **Step 7: Commit**

```bash
git add iLearn/ViewModels/Pages/VideoDownloadListViewModel.cs iLearn/Views/Pages/VideoDownloadListPage.xaml
git commit -m "feat: improve video download selection flow"
```

## Task 5: Download Manager UI And Speed Display

**Files:**
- Modify: `iLearn/ViewModels/Pages/DownloadManageViewModel.cs`
- Modify: `iLearn/Views/Pages/DownloadManagePage.xaml`

- [x] **Step 1: Use status constants in the view model**

Replace string comparisons in `DownloadManageViewModel` with constants:

```csharp
ActiveDownloadsCount = Downloads.Count(d => d.Status == DownloadStatus.Downloading);
CompletedDownloadsCount = Downloads.Count(d => d.Status == DownloadStatus.Completed);
QueuedDownloadsCount = Downloads.Count(d => d.Status is DownloadStatus.Queued or DownloadStatus.Waiting) + _downloadService.GetQueuedDownloadsCount();
HasDownloadingItems = Downloads.Any(d => d.Status == DownloadStatus.Downloading);
HasPausedItems = Downloads.Any(d => d.Status == DownloadStatus.Paused);
```

Update sorting:

```csharp
private static int GetStatusSortOrder(string status) =>
    status switch
    {
        DownloadStatus.Failed => 0,
        DownloadStatus.Downloading => 1,
        DownloadStatus.Paused => 2,
        DownloadStatus.Queued => 3,
        DownloadStatus.Waiting => 4,
        DownloadStatus.Completed => 5,
        DownloadStatus.Cancelled => 6,
        _ => 7
    };
```

- [x] **Step 2: Format total speed consistently**

Add this helper to `DownloadManageViewModel`:

```csharp
private static string FormatBytesPerSecond(double bytesPerSecond)
{
    if (bytesPerSecond >= 1024 * 1024)
        return $"{bytesPerSecond / 1024 / 1024:0.##} MB/s";
    if (bytesPerSecond >= 1024)
        return $"{bytesPerSecond / 1024:0.##} KB/s";
    return $"{bytesPerSecond:0} B/s";
}
```

Replace total speed calculation with:

```csharp
var totalSpeedBytes = Downloads
    .Where(d => d.Status == DownloadStatus.Downloading)
    .Sum(d => d.SpeedValue);

TotalDownloadSpeed = FormatBytesPerSecond(totalSpeedBytes);
```

- [x] **Step 3: Add a compact status template**

Replace the `StatusTemplate` in `DownloadManagePage.xaml` with:

```xml
<DataTemplate x:Key="StatusTemplate">
    <Border CornerRadius="4" Padding="8,3" Background="{DynamicResource ControlFillColorSecondaryBrush}">
        <TextBlock Text="{Binding Status}"
                   FontSize="11"
                   FontWeight="SemiBold"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}"/>
    </Border>
</DataTemplate>
```

- [x] **Step 4: Replace row metadata with speed, size, and remaining time**

Replace the `下载信息` grid in `DownloadManagePage.xaml` with:

```xml
<Grid Grid.Row="2" Margin="0,0,0,12">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="140"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="100"/>
    </Grid.ColumnDefinitions>

    <TextBlock Grid.Column="0"
               Text="{Binding SpeedValue, Converter={StaticResource SpeedConverter}}"
               FontSize="12"
               Foreground="{DynamicResource TextFillColorSecondaryBrush}"/>
    <TextBlock Grid.Column="1"
               Text="{Binding SizeText}"
               FontSize="12"
               Foreground="{DynamicResource TextFillColorSecondaryBrush}"
               TextTrimming="CharacterEllipsis"/>
    <TextBlock Grid.Column="2"
               Text="{Binding RemainingText}"
               FontSize="12"
               HorizontalAlignment="Right"
               Foreground="{DynamicResource TextFillColorSecondaryBrush}"/>
</Grid>
```

- [x] **Step 5: Use icon-sized row actions**

For each row action button, remove `Content`, keep `Icon`, and set fixed dimensions:

```xml
<ui:Button Icon="Pause24"
           Width="36"
           Height="32"
           Command="{Binding DataContext.PauseDownloadCommand, RelativeSource={RelativeSource AncestorType=Page}}"
           CommandParameter="{Binding}"
           Appearance="Secondary"
           Margin="0,0,8,0"/>
```

Apply the same `Width="36"` and `Height="32"` pattern to resume, retry, cancel, and open-file buttons.

- [x] **Step 6: Run build**

Run: `dotnet build iLearn.sln --configuration Debug -v minimal`

Expected: build succeeds. The download manager shows total speed in B/s, KB/s, or MB/s instead of forcing `0.00 MB/s`.

- [x] **Step 7: Commit**

```bash
git add iLearn/ViewModels/Pages/DownloadManageViewModel.cs iLearn/Views/Pages/DownloadManagePage.xaml
git commit -m "feat: improve download manager status display"
```

## Task 6: Automatic Subtitle Loading In Local Playback

**Files:**
- Modify: `iLearn/ViewModels/Pages/LocalVideoViewModel.cs`
- Modify: `iLearn/Assets/VideoPlayer.html`

- [x] **Step 1: Inject subtitle path into player HTML**

Modify `OpenLocalVideoAsync` in `LocalVideoViewModel`:

```csharp
private async Task OpenLocalVideoAsync(LocalVideoFile video)
{
    var uri = new Uri("pack://application:,,,/Assets/VideoPlayer.html");
    var streamInfo = Application.GetResourceStream(uri);
    if (streamInfo == null)
        throw new FileNotFoundException("无法加载内置播放器模板。");

    using var reader = new StreamReader(streamInfo.Stream);
    var content = await reader.ReadToEndAsync();

    content = content.Replace("_LEFTVIDEO_", new Uri(video.FullPath).AbsoluteUri);

    var partnerVideo = video.GetPartnerVideo();
    content = content.Replace("_RIGHTVIDEO_", partnerVideo == null ? new Uri(video.FullPath).AbsoluteUri : new Uri(partnerVideo.FullPath).AbsoluteUri);

    var subtitlePath = video.FindSubtitlePath(_appConfig.DownloadPath);
    content = content.Replace("_SUBTITLE_", subtitlePath == null ? string.Empty : new Uri(subtitlePath).AbsoluteUri);

    string tempFile = Path.Combine(Path.GetTempPath(), $"video_local_{Guid.NewGuid()}.html");
    await File.WriteAllTextAsync(tempFile, content);

    Process.Start(new ProcessStartInfo
    {
        FileName = tempFile,
        UseShellExecute = true
    });
}
```

- [x] **Step 2: Show a subtitle-aware snackbar**

In `OpenVideo`, replace the current info snackbar with:

```csharp
var subtitlePath = video.FindSubtitlePath(_appConfig.DownloadPath);
ShowSnackbar(
    "正在打开",
    subtitlePath == null ? $"正在打开 {video.FileName}，未找到匹配字幕" : $"正在打开 {video.FileName}，已自动匹配字幕",
    ControlAppearance.Info);
```

- [x] **Step 3: Ensure player loads local subtitle URL**

In `iLearn/Assets/VideoPlayer.html`, keep this startup call:

```javascript
loadRemoteSubtitle('_SUBTITLE_');
```

Replace `loadRemoteSubtitle` with:

```javascript
function loadRemoteSubtitle(url) {
    if (!url || url === '_SUBTITLE_') return;
    fetch(url)
        .then(r => {
            if (!r.ok) throw new Error('subtitle request failed');
            return r.text();
        })
        .then(text => {
            state.subtitles = parseVTT(text);
            renderSubtitleList();
            els.subStatus.textContent = '自动字幕';
            els.subStatus.style.color = 'var(--primary)';
            state.isSubVisible = true;
            $('#sub-toggle-btn').classList.add('active');
            renderChapterMarks();
            showToast(`已加载 ${state.subtitles.length} 条字幕`);
        })
        .catch(() => {
            els.subStatus.textContent = '无字幕';
        });
}
```

- [x] **Step 4: Run build**

Run: `dotnet build iLearn.sln --configuration Debug -v minimal`

Expected: build succeeds.

- [x] **Step 5: Manual runtime check**

Use the app to download one video and its subtitle. Open the local video page and click that video.

Expected: the generated temporary HTML contains `file:///.../Subtitles/<video>.vtt`, the subtitle toggle is active, and captions appear without choosing a file manually. Manual subtitle upload still works when no matching subtitle exists.

- [x] **Step 6: Commit**

```bash
git add iLearn/ViewModels/Pages/LocalVideoViewModel.cs iLearn/Assets/VideoPlayer.html
git commit -m "feat: auto-load local video subtitles"
```

## Task 7: Full Verification

**Files:**
- No new files.

- [x] **Step 1: Restore**

Run: `dotnet restore iLearn.sln`

Expected: restore succeeds.

- [x] **Step 2: Run tests**

Run: `dotnet test iLearn.sln --configuration Debug -v minimal`

Expected: all tests pass.

- [x] **Step 3: Debug build**

Run: `dotnet build iLearn.sln --configuration Debug -v minimal`

Expected: build succeeds.

- [x] **Step 4: Release build**

Run: `dotnet build iLearn.sln --configuration Release -v minimal`

Expected: build succeeds.

- [x] **Step 5: Runtime smoke test**

Run the WPF app, select one HDMI and one 教师 video, add to download, open the download manager, pause/resume/cancel one task, let one task finish, then open it from the local video page.

Expected: selected count updates before queueing, the download manager speed and ETA update every second, row buttons do not jump layout, completed files open, and matching subtitles load automatically in the HTML player.

- [x] **Step 6: Commit verification-only adjustments**

If verification required small fixes, commit them:

```bash
git add iLearn iLearn.Tests
git commit -m "fix: resolve download subtitle verification issues"
```

If no fixes were needed, do not create an empty commit.

## Self-Review

- Spec coverage: Task 3 and Task 4 address hard-to-use download logic; Task 5 addresses speed display and control styling; Task 6 addresses manual subtitle selection for local video playback.
- Placeholder scan: the plan contains no `TBD`, `TODO`, or undefined future work. Every implementation step includes code or exact XAML/command text.
- Type consistency: `FileNameService`, `DownloadStatus`, `FindSubtitlePath`, nullable `GetPartnerVideo`, and `StartDownloadAsync(..., perspective)` are introduced before later tasks depend on them.

