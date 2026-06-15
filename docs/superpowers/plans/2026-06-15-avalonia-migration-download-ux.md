# Avalonia Migration and Download UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate iLearn from Windows-only WPF/WPF-UI to a cross-platform Avalonia desktop app, while redesigning downloads and user feedback so everyday course viewing and offline use feel smooth on Windows, macOS, and Linux.

**Architecture:** Keep the existing solution shape but replace WPF views with Avalonia views and move reusable logic into UI-neutral services. Use CommunityToolkit.Mvvm for view models, Microsoft.Extensions.DependencyInjection for composition, Semi.Avalonia + Ursa.Avalonia for a clean non-Fluent UI, and a dedicated download engine with explicit queue state, progress events, retry, pause/resume, and notifications.

**Tech Stack:** .NET 10, Avalonia 11/12-compatible packages selected by NuGet restore, Semi.Avalonia, Irihi.Ursa, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, LiteDB, Polly, HtmlAgilityPack, xUnit v3, System.Text.Json source-friendly DTOs.

---

## Current State Summary

- `iLearn/iLearn.csproj` targets `net10.0-windows`, has `<UseWPF>true</UseWPF>`, and depends on `WPF-UI`, `Microsoft.Xaml.Behaviors.Wpf`, `AutoUpdater.NET.Official`, and `Downloader`.
- UI files live under `iLearn/Views/**/*.xaml` and use WPF page/window patterns.
- View models already use `CommunityToolkit.Mvvm`, which can be retained.
- `AppConfig` uses Windows DPAPI via `System.Security.Cryptography.ProtectedData`; this must be replaced for macOS/Linux.
- Download management is in `VideoDownloadService`, `VideoDownloadListViewModel`, `DownloadManageViewModel`, and `SettingViewModel`.
- Existing tests are in `iLearn.Tests`, but the test project also targets `net10.0-windows`; it must become cross-platform.

## Chosen UI Library

Use **Semi.Avalonia + Irihi.Ursa** as the primary UI stack.

Reasons:
- Semi.Avalonia is a full independent Avalonia theme, so the app does not need FluentTheme.
- Ursa adds practical app controls such as notification, dialog, drawer, path picker, form, and other desktop-oriented controls that the current WPF-UI app relies on conceptually.
- The visual direction is modern, crisp, and app-like without copying FluentUI.

Keep **Material.Avalonia** as a fallback only if Semi/Ursa packages fail to restore or block a core control.

## Target File Structure

Create:
- `iLearn/App.axaml` - Avalonia application styles and theme registration.
- `iLearn/App.axaml.cs` - Avalonia app startup and DI bootstrapping.
- `iLearn/Program.cs` - Avalonia desktop entry point.
- `iLearn/Composition/ServiceCollectionExtensions.cs` - DI registration for views, view models, services, navigation, notifications, and platform services.
- `iLearn/Navigation/AppRoute.cs` - Route enum for main shell pages.
- `iLearn/Navigation/NavigationItemViewModel.cs` - Sidebar item model with icon, label, route, and active state.
- `iLearn/Navigation/NavigationService.cs` - UI-neutral current-page navigation service.
- `iLearn/Notifications/AppNotification.cs` - Notification DTO.
- `iLearn/Notifications/INotificationService.cs` - Notification interface for view models.
- `iLearn/Notifications/NotificationService.cs` - Observable notification store.
- `iLearn/Platform/IPlatformLauncher.cs` - Cross-platform file, folder, URL opener.
- `iLearn/Platform/PlatformLauncher.cs` - Windows/macOS/Linux launch implementation.
- `iLearn/Security/ISecretStore.cs` - Cross-platform credential storage boundary.
- `iLearn/Security/FileSecretStore.cs` - Local encrypted-or-obfuscated credential fallback with clear reset behavior.
- `iLearn/Downloads/DownloadTaskStatus.cs` - Strong enum for download states.
- `iLearn/Downloads/DownloadTaskSnapshot.cs` - Immutable download state sent to UI.
- `iLearn/Downloads/DownloadRequest.cs` - Queue request data.
- `iLearn/Downloads/IDownloadEngine.cs` - Download engine interface.
- `iLearn/Downloads/HttpRangeDownloadEngine.cs` - Cross-platform HTTP range download engine.
- `iLearn/Downloads/DownloadQueueService.cs` - Queue, pause, resume, retry, cancel, speed limit, concurrency.
- `iLearn/ViewModels/AppViewModelBase.cs` - Base observable object with busy/status helpers.
- `iLearn/ViewModels/ShellViewModel.cs` - Main shell navigation state.
- `iLearn/ViewModels/Pages/*.cs` - Avalonia-compatible page view models, migrated from existing WPF view models.
- `iLearn/ViewModels/Windows/LoginViewModel.cs` - Replace WPF bitmap/snackbar dependencies with Avalonia-friendly state and notifications.
- `iLearn/Views/Windows/LoginWindow.axaml`
- `iLearn/Views/Windows/LoginWindow.axaml.cs`
- `iLearn/Views/Windows/MainWindow.axaml`
- `iLearn/Views/Windows/MainWindow.axaml.cs`
- `iLearn/Views/Pages/CoursesPage.axaml`
- `iLearn/Views/Pages/CoursesPage.axaml.cs`
- `iLearn/Views/Pages/MediaPage.axaml`
- `iLearn/Views/Pages/MediaPage.axaml.cs`
- `iLearn/Views/Pages/VideoDownloadListPage.axaml`
- `iLearn/Views/Pages/VideoDownloadListPage.axaml.cs`
- `iLearn/Views/Pages/DownloadManagePage.axaml`
- `iLearn/Views/Pages/DownloadManagePage.axaml.cs`
- `iLearn/Views/Pages/LocalVideoPage.axaml`
- `iLearn/Views/Pages/LocalVideoPage.axaml.cs`
- `iLearn/Views/Pages/SettingPage.axaml`
- `iLearn/Views/Pages/SettingPage.axaml.cs`
- `iLearn/Updates/UpdateManifest.cs`
- `iLearn/Updates/UpdateCheckResult.cs`
- `iLearn/Updates/IUpdateService.cs`
- `iLearn/Updates/UpdateService.cs`
- `iLearn.Tests/Downloads/DownloadQueueServiceTests.cs`
- `iLearn.Tests/Navigation/NavigationServiceTests.cs`
- `iLearn.Tests/Platform/PlatformLauncherTests.cs`
- `iLearn.Tests/Updates/UpdateServiceTests.cs`

Modify:
- `iLearn/iLearn.csproj` - remove WPF/Windows-only settings and add Avalonia/Semi/Ursa packages.
- `iLearn.Tests/iLearn.Tests.csproj` - target `net10.0`, not `net10.0-windows`.
- `iLearn.sln` and `iLearn.slnx` - keep project references valid after removing WPF-only files.
- `README.md` - update branding from Fluent/WPF to Avalonia/Semi and cross-platform release notes.

Delete after replacement is compiling:
- `App.xaml`
- `iLearn/App.xaml`
- `iLearn/App.xaml.cs`
- `iLearn/Views/**/*.xaml`
- WPF-only code-behind files after their `.axaml.cs` equivalents exist.
- `iLearn/Behaviors/MouseWheelToScrollBehavior.cs`
- `iLearn/Services/WindowsManagerService.cs`
- `iLearn/Services/UpdateService.cs` after replacement in `iLearn/Updates/`.

---

### Task 1: Convert Projects to Cross-Platform Avalonia Baseline

**Files:**
- Modify: `iLearn/iLearn.csproj`
- Modify: `iLearn.Tests/iLearn.Tests.csproj`
- Create: `iLearn/Program.cs`
- Create: `iLearn/App.axaml`
- Create: `iLearn/App.axaml.cs`

- [ ] **Step 1: Update application project dependencies**

Replace `iLearn/iLearn.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>iLearn-icon.ico</ApplicationIcon>
    <AssemblyVersion>1.3.0.0</AssemblyVersion>
    <FileVersion>1.3.0.0</FileVersion>
    <Version>1.3.0</Version>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.*" />
    <PackageReference Include="Avalonia.Desktop" Version="11.*" />
    <PackageReference Include="Avalonia.Themes.Simple" Version="11.*" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.*" />
    <PackageReference Include="Avalonia.Diagnostics" Version="11.*" Condition="'$(Configuration)' == 'Debug'" />
    <PackageReference Include="Semi.Avalonia" Version="12.*" />
    <PackageReference Include="Irihi.Ursa" Version="1.*" />
    <PackageReference Include="Irihi.Ursa.Themes.Semi" Version="1.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="HtmlAgilityPack" Version="1.12.2" />
    <PackageReference Include="LiteDB" Version="5.0.21" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.8" />
    <PackageReference Include="Polly" Version="8.6.3" />
    <PackageReference Include="System.Runtime.Caching" Version="9.0.8" />
  </ItemGroup>

  <ItemGroup>
    <AvaloniaResource Include="Assets/**" />
    <Content Include="iLearn-icon.ico" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Update test project target**

Replace `iLearn.Tests/iLearn.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
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

- [ ] **Step 3: Add Avalonia entry point**

Create `iLearn/Program.cs`:

```csharp
using Avalonia;
using System;

namespace iLearn;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
```

- [ ] **Step 4: Add Avalonia app resources**

Create `iLearn/App.axaml`:

```xml
<Application
    x:Class="iLearn.App"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Application.Styles>
    <SemiTheme Locale="zh-CN" />
    <UrsaTheme Locale="zh-CN" />
  </Application.Styles>
</Application>
```

- [ ] **Step 5: Add minimal startup code**

Create `iLearn/App.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using iLearn.Composition;
using iLearn.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace iLearn;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services =>
        ((App)Current!)._host?.Services
        ?? throw new InvalidOperationException("Application services are not initialized.");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => services.AddILearnApp())
            .Build();

        _host.StartAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<LoginWindow>();
            desktop.Exit += async (_, _) => await StopHostAsync();
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                WriteCrashLog(exception);
        };

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StopHostAsync()
    {
        if (_host is null)
            return;

        await _host.StopAsync();
        _host.Dispose();
    }

    private static void WriteCrashLog(Exception exception)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "iLearn",
            "logs");
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, $"error_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        File.AppendAllText(file, $"[{DateTime.Now:O}] {exception}\n");
    }
}
```

- [ ] **Step 6: Run restore to verify package selection**

Run:

```bash
dotnet restore iLearn.sln
```

Expected: restore succeeds. If NuGet cannot resolve `12.*` for `Semi.Avalonia`, run `dotnet list iLearn/iLearn.csproj package --outdated`, choose the newest stable Semi package compatible with the restored Avalonia major version, and pin that exact version in `iLearn/iLearn.csproj`.

- [ ] **Step 7: Commit**

```bash
git add iLearn/iLearn.csproj iLearn.Tests/iLearn.Tests.csproj iLearn/Program.cs iLearn/App.axaml iLearn/App.axaml.cs
git commit -m "build: start avalonia migration baseline"
```

---

### Task 2: Add DI, Navigation, Notifications, and Platform Services

**Files:**
- Create: `iLearn/Composition/ServiceCollectionExtensions.cs`
- Create: `iLearn/Navigation/AppRoute.cs`
- Create: `iLearn/Navigation/NavigationItemViewModel.cs`
- Create: `iLearn/Navigation/NavigationService.cs`
- Create: `iLearn/Notifications/AppNotification.cs`
- Create: `iLearn/Notifications/INotificationService.cs`
- Create: `iLearn/Notifications/NotificationService.cs`
- Create: `iLearn/Platform/IPlatformLauncher.cs`
- Create: `iLearn/Platform/PlatformLauncher.cs`
- Create: `iLearn.Tests/Navigation/NavigationServiceTests.cs`

- [ ] **Step 1: Write navigation test**

Create `iLearn.Tests/Navigation/NavigationServiceTests.cs`:

```csharp
using iLearn.Navigation;
using Xunit;

namespace iLearn.Tests.Navigation;

public sealed class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_ChangesCurrentRoute_AndRaisesEvent()
    {
        var service = new NavigationService();
        AppRoute? observed = null;

        service.RouteChanged += (_, route) => observed = route;

        service.NavigateTo(AppRoute.Downloads);

        Assert.Equal(AppRoute.Downloads, service.CurrentRoute);
        Assert.Equal(AppRoute.Downloads, observed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~NavigationServiceTests
```

Expected: FAIL because `iLearn.Navigation` types do not exist.

- [ ] **Step 3: Add route enum**

Create `iLearn/Navigation/AppRoute.cs`:

```csharp
namespace iLearn.Navigation;

public enum AppRoute
{
    Courses,
    Media,
    DownloadSelection,
    Downloads,
    LocalVideos,
    Settings
}
```

- [ ] **Step 4: Add navigation item model**

Create `iLearn/Navigation/NavigationItemViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace iLearn.Navigation;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(AppRoute route, string title, string iconKey)
    {
        Route = route;
        Title = title;
        IconKey = iconKey;
    }

    public AppRoute Route { get; }
    public string Title { get; }
    public string IconKey { get; }

    [ObservableProperty]
    private bool _isSelected;
}
```

- [ ] **Step 5: Add navigation service**

Create `iLearn/Navigation/NavigationService.cs`:

```csharp
namespace iLearn.Navigation;

public sealed class NavigationService
{
    public event EventHandler<AppRoute>? RouteChanged;

    public AppRoute CurrentRoute { get; private set; } = AppRoute.Courses;

    public void NavigateTo(AppRoute route)
    {
        if (CurrentRoute == route)
            return;

        CurrentRoute = route;
        RouteChanged?.Invoke(this, route);
    }
}
```

- [ ] **Step 6: Add notification DTO and service**

Create `iLearn/Notifications/AppNotification.cs`:

```csharp
namespace iLearn.Notifications;

public sealed record AppNotification(
    string Title,
    string Message,
    AppNotificationKind Kind,
    DateTimeOffset CreatedAt);

public enum AppNotificationKind
{
    Info,
    Success,
    Warning,
    Error
}
```

Create `iLearn/Notifications/INotificationService.cs`:

```csharp
using System.Collections.ObjectModel;

namespace iLearn.Notifications;

public interface INotificationService
{
    ReadOnlyObservableCollection<AppNotification> Items { get; }
    void Show(string title, string message, AppNotificationKind kind);
    void Clear(AppNotification notification);
}
```

Create `iLearn/Notifications/NotificationService.cs`:

```csharp
using System.Collections.ObjectModel;

namespace iLearn.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly ObservableCollection<AppNotification> _items = new();

    public NotificationService()
    {
        Items = new ReadOnlyObservableCollection<AppNotification>(_items);
    }

    public ReadOnlyObservableCollection<AppNotification> Items { get; }

    public void Show(string title, string message, AppNotificationKind kind)
    {
        _items.Insert(0, new AppNotification(title, message, kind, DateTimeOffset.Now));
        while (_items.Count > 5)
            _items.RemoveAt(_items.Count - 1);
    }

    public void Clear(AppNotification notification)
    {
        _items.Remove(notification);
    }
}
```

- [ ] **Step 7: Add cross-platform launcher**

Create `iLearn/Platform/IPlatformLauncher.cs`:

```csharp
namespace iLearn.Platform;

public interface IPlatformLauncher
{
    Task OpenFileAsync(string path, CancellationToken cancellationToken = default);
    Task OpenFolderAsync(string path, CancellationToken cancellationToken = default);
    Task OpenUrlAsync(string url, CancellationToken cancellationToken = default);
}
```

Create `iLearn/Platform/PlatformLauncher.cs`:

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace iLearn.Platform;

public sealed class PlatformLauncher : IPlatformLauncher
{
    public Task OpenFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File does not exist.", path);

        return StartAsync(path, cancellationToken);
    }

    public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return StartAsync(path, cancellationToken);
    }

    public Task OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("URL must be absolute.", nameof(url));

        return StartAsync(url, cancellationToken);
    }

    private static Task StartAsync(string target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo(target) { UseShellExecute = true }
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? new ProcessStartInfo("open", EscapeArgument(target))
                : new ProcessStartInfo("xdg-open", EscapeArgument(target));

        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private static string EscapeArgument(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
```

- [ ] **Step 8: Add DI registration**

Create `iLearn/Composition/ServiceCollectionExtensions.cs`:

```csharp
using iLearn.Models;
using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.Platform;
using iLearn.Services;
using iLearn.ViewModels;
using iLearn.ViewModels.Pages;
using iLearn.ViewModels.Windows;
using iLearn.Views.Pages;
using iLearn.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace iLearn.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddILearnApp(this IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iLearn",
                "config.json");
            return new AppConfig(configPath);
        });

        services.AddSingleton<ILearnApiService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPlatformLauncher, PlatformLauncher>();

        services.AddSingleton<LoginWindow>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ShellViewModel>();

        services.AddSingleton<CoursesPage>();
        services.AddSingleton<CoursesViewModel>();
        services.AddSingleton<MediaPage>();
        services.AddSingleton<MediaViewModel>();
        services.AddSingleton<VideoDownloadListPage>();
        services.AddSingleton<VideoDownloadListViewModel>();
        services.AddSingleton<DownloadManagePage>();
        services.AddSingleton<DownloadManageViewModel>();
        services.AddSingleton<LocalVideoPage>();
        services.AddSingleton<LocalVideoViewModel>();
        services.AddSingleton<SettingPage>();
        services.AddSingleton<SettingViewModel>();

        services.AddSingleton<List<LiveAndRecordInfo>>();

        return services;
    }
}
```

- [ ] **Step 9: Run navigation tests**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~NavigationServiceTests
```

Expected: PASS.

- [ ] **Step 10: Commit**

```bash
git add iLearn/Composition iLearn/Navigation iLearn/Notifications iLearn/Platform iLearn.Tests/Navigation
git commit -m "feat: add avalonia app infrastructure"
```

---

### Task 3: Replace Windows Credential Storage and AppConfig

**Files:**
- Create: `iLearn/Security/ISecretStore.cs`
- Create: `iLearn/Security/FileSecretStore.cs`
- Modify: `iLearn/Models/AppConfig.cs`
- Create: `iLearn.Tests/Security/FileSecretStoreTests.cs`

- [ ] **Step 1: Write secret store tests**

Create `iLearn.Tests/Security/FileSecretStoreTests.cs`:

```csharp
using iLearn.Security;
using Xunit;

namespace iLearn.Tests.Security;

public sealed class FileSecretStoreTests
{
    [Fact]
    public async Task SaveAndReadSecret_RoundTripsValue()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-secret-tests", Guid.NewGuid().ToString("N"));
        var store = new FileSecretStore(Path.Combine(directory, "secrets.json"));

        await store.SaveSecretAsync("login-password", "secret-value");
        var value = await store.ReadSecretAsync("login-password");

        Assert.Equal("secret-value", value);
    }

    [Fact]
    public async Task DeleteSecret_RemovesValue()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-secret-tests", Guid.NewGuid().ToString("N"));
        var store = new FileSecretStore(Path.Combine(directory, "secrets.json"));

        await store.SaveSecretAsync("login-password", "secret-value");
        await store.DeleteSecretAsync("login-password");

        Assert.Null(await store.ReadSecretAsync("login-password"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~FileSecretStoreTests
```

Expected: FAIL because `iLearn.Security` types do not exist.

- [ ] **Step 3: Add secret store interface**

Create `iLearn/Security/ISecretStore.cs`:

```csharp
namespace iLearn.Security;

public interface ISecretStore
{
    Task<string?> ReadSecretAsync(string key, CancellationToken cancellationToken = default);
    Task SaveSecretAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Add file secret store**

Create `iLearn/Security/FileSecretStore.cs`:

```csharp
using System.Text;
using System.Text.Json;

namespace iLearn.Security;

public sealed class FileSecretStore : ISecretStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileSecretStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<string?> ReadSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadAllAsync(cancellationToken);
            return data.TryGetValue(key, out var encoded)
                ? Encoding.UTF8.GetString(Convert.FromBase64String(encoded))
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadAllAsync(cancellationToken);
            data[key] = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            await WriteAllAsync(data, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadAllAsync(cancellationToken);
            data.Remove(key);
            await WriteAllAsync(data, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, string>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, string>();

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken)
            ?? new Dictionary<string, string>();
    }

    private async Task WriteAllAsync(Dictionary<string, string> data, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }
}
```

- [ ] **Step 5: Simplify AppConfig for cross-platform config**

Replace password-related properties in `iLearn/Models/AppConfig.cs` with these properties and keep existing download settings:

```csharp
public string? UserName { get; set; }
public bool IsRememberMeEnabled { get; set; } = false;
public bool IsAutoLoginEnabled { get; set; } = false;
public int MaxConcurrentDownloads { get; set; } = 3;
public int ChunkCount { get; set; } = 8;
public long SpeedLimitBytesPerSecond { get; set; } = 0;
public string DownloadPath { get; set; } =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "iLearnVideo");
```

Remove these members from `AppConfig`:

```csharp
[JsonIgnore]
public string? UserPassword { get; set; }

public string? EncryptedUserPassword { get; set; }
```

In the file-load copy block, copy only:

```csharp
UserName = config.UserName;
IsRememberMeEnabled = config.IsRememberMeEnabled;
IsAutoLoginEnabled = config.IsAutoLoginEnabled;
MaxConcurrentDownloads = config.MaxConcurrentDownloads;
ChunkCount = config.ChunkCount;
SpeedLimitBytesPerSecond = config.SpeedLimitBytesPerSecond;
DownloadPath = string.IsNullOrWhiteSpace(config.DownloadPath)
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "iLearnVideo")
    : config.DownloadPath;
```

- [ ] **Step 6: Register secret store**

In `iLearn/Composition/ServiceCollectionExtensions.cs`, add:

```csharp
using iLearn.Security;
```

Then register:

```csharp
services.AddSingleton<ISecretStore>(_ =>
{
    var secretsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "iLearn",
        "secrets.json");
    return new FileSecretStore(secretsPath);
});
```

- [ ] **Step 7: Run security tests**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~FileSecretStoreTests
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add iLearn/Security iLearn/Models/AppConfig.cs iLearn/Composition/ServiceCollectionExtensions.cs iLearn.Tests/Security
git commit -m "feat: replace windows-only credential storage"
```

---

### Task 4: Build Download Domain and Queue Tests

**Files:**
- Create: `iLearn/Downloads/DownloadTaskStatus.cs`
- Create: `iLearn/Downloads/DownloadTaskSnapshot.cs`
- Create: `iLearn/Downloads/DownloadRequest.cs`
- Create: `iLearn/Downloads/IDownloadEngine.cs`
- Create: `iLearn/Downloads/DownloadQueueService.cs`
- Create: `iLearn.Tests/Downloads/DownloadQueueServiceTests.cs`

- [ ] **Step 1: Write queue behavior tests**

Create `iLearn.Tests/Downloads/DownloadQueueServiceTests.cs`:

```csharp
using iLearn.Downloads;
using Xunit;

namespace iLearn.Tests.Downloads;

public sealed class DownloadQueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_PublishesQueuedSnapshot()
    {
        var engine = new FakeDownloadEngine();
        var service = new DownloadQueueService(engine);

        var request = new DownloadRequest(
            Id: "task-1",
            Url: "https://example.test/video.mp4",
            FileName: "video.mp4",
            OutputDirectory: Path.GetTempPath(),
            DisplayName: "第一讲",
            Perspective: "HDMI");

        await service.EnqueueAsync(request);

        var snapshot = Assert.Single(service.Tasks);
        Assert.Equal("task-1", snapshot.Id);
        Assert.Equal(DownloadTaskStatus.Queued, snapshot.Status);
    }

    [Fact]
    public async Task CancelAsync_MarksQueuedTaskCancelled()
    {
        var engine = new FakeDownloadEngine();
        var service = new DownloadQueueService(engine);

        await service.EnqueueAsync(new DownloadRequest(
            "task-1",
            "https://example.test/video.mp4",
            "video.mp4",
            Path.GetTempPath(),
            "第一讲",
            "HDMI"));

        await service.CancelAsync("task-1");

        var snapshot = Assert.Single(service.Tasks);
        Assert.Equal(DownloadTaskStatus.Cancelled, snapshot.Status);
    }

    private sealed class FakeDownloadEngine : IDownloadEngine
    {
        public Task DownloadAsync(
            DownloadRequest request,
            string outputPath,
            IProgress<DownloadTaskSnapshot> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(DownloadTaskSnapshot.Downloading(request, 10, 100, 1024, null));
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~DownloadQueueServiceTests
```

Expected: FAIL because download domain types do not exist.

- [ ] **Step 3: Add status enum**

Create `iLearn/Downloads/DownloadTaskStatus.cs`:

```csharp
namespace iLearn.Downloads;

public enum DownloadTaskStatus
{
    Waiting,
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
    Cancelled
}
```

- [ ] **Step 4: Add request and snapshot types**

Create `iLearn/Downloads/DownloadRequest.cs`:

```csharp
namespace iLearn.Downloads;

public sealed record DownloadRequest(
    string Id,
    string Url,
    string FileName,
    string OutputDirectory,
    string DisplayName,
    string Perspective);
```

Create `iLearn/Downloads/DownloadTaskSnapshot.cs`:

```csharp
namespace iLearn.Downloads;

public sealed record DownloadTaskSnapshot(
    string Id,
    string Url,
    string FileName,
    string OutputPath,
    string DisplayName,
    string Perspective,
    DownloadTaskStatus Status,
    long BytesReceived,
    long TotalBytes,
    double BytesPerSecond,
    string? ErrorMessage)
{
    public double Progress => TotalBytes <= 0 ? 0 : Math.Clamp(BytesReceived * 100.0 / TotalBytes, 0, 100);

    public static DownloadTaskSnapshot FromRequest(DownloadRequest request, DownloadTaskStatus status, string? errorMessage = null)
    {
        return new DownloadTaskSnapshot(
            request.Id,
            request.Url,
            request.FileName,
            Path.Combine(request.OutputDirectory, request.FileName),
            request.DisplayName,
            request.Perspective,
            status,
            0,
            0,
            0,
            errorMessage);
    }

    public static DownloadTaskSnapshot Downloading(
        DownloadRequest request,
        long bytesReceived,
        long totalBytes,
        double bytesPerSecond,
        string? errorMessage)
    {
        return new DownloadTaskSnapshot(
            request.Id,
            request.Url,
            request.FileName,
            Path.Combine(request.OutputDirectory, request.FileName),
            request.DisplayName,
            request.Perspective,
            DownloadTaskStatus.Downloading,
            bytesReceived,
            totalBytes,
            bytesPerSecond,
            errorMessage);
    }
}
```

- [ ] **Step 5: Add download engine interface**

Create `iLearn/Downloads/IDownloadEngine.cs`:

```csharp
namespace iLearn.Downloads;

public interface IDownloadEngine
{
    Task DownloadAsync(
        DownloadRequest request,
        string outputPath,
        IProgress<DownloadTaskSnapshot> progress,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Add queue service**

Create `iLearn/Downloads/DownloadQueueService.cs`:

```csharp
using System.Collections.ObjectModel;

namespace iLearn.Downloads;

public sealed class DownloadQueueService
{
    private readonly IDownloadEngine _engine;
    private readonly ObservableCollection<DownloadTaskSnapshot> _tasks = new();
    private readonly Dictionary<string, DownloadRequest> _requests = new();
    private readonly Dictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DownloadQueueService(IDownloadEngine engine)
    {
        _engine = engine;
        Tasks = new ReadOnlyObservableCollection<DownloadTaskSnapshot>(_tasks);
    }

    public ReadOnlyObservableCollection<DownloadTaskSnapshot> Tasks { get; }

    public async Task EnqueueAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(request.OutputDirectory);
            _requests[request.Id] = request;
            Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Queued));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CancelAsync(string id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cancellations.Remove(id, out var cts))
                await cts.CancelAsync();

            if (_requests.TryGetValue(id, out var request))
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Cancelled));
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task PauseAsync(string id, CancellationToken cancellationToken = default)
    {
        return CancelAsync(id, cancellationToken);
    }

    public async Task RetryAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_requests.TryGetValue(id, out var request))
            throw new InvalidOperationException($"Download task '{id}' does not exist.");

        await EnqueueAsync(request, cancellationToken);
    }

    private void Upsert(DownloadTaskSnapshot snapshot)
    {
        var index = _tasks.Select((item, i) => (item, i))
            .FirstOrDefault(pair => pair.item.Id == snapshot.Id).i;

        if (_tasks.Any(item => item.Id == snapshot.Id))
            _tasks[index] = snapshot;
        else
            _tasks.Add(snapshot);
    }
}
```

- [ ] **Step 7: Keep DI unchanged for this task**

Do not register `DownloadQueueService` yet. Task 5 creates `HttpRangeDownloadEngine`; registering `IDownloadEngine` before that file exists will make this task fail to compile.

- [ ] **Step 8: Run queue tests**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~DownloadQueueServiceTests
```

Expected: PASS for queue state tests.

- [ ] **Step 9: Commit**

```bash
git add iLearn/Downloads iLearn/Composition/ServiceCollectionExtensions.cs iLearn.Tests/Downloads
git commit -m "feat: add download queue domain"
```

---

### Task 5: Implement Cross-Platform HTTP Download Engine

**Files:**
- Create: `iLearn/Downloads/HttpRangeDownloadEngine.cs`
- Modify: `iLearn/Downloads/DownloadQueueService.cs`
- Create: `iLearn.Tests/Downloads/HttpRangeDownloadEngineTests.cs`

- [ ] **Step 1: Write engine smoke test with local HTTP server**

Create `iLearn.Tests/Downloads/HttpRangeDownloadEngineTests.cs`:

```csharp
using iLearn.Downloads;
using Xunit;

namespace iLearn.Tests.Downloads;

public sealed class HttpRangeDownloadEngineTests
{
    [Fact]
    public async Task DownloadAsync_WritesFile_AndReportsCompletionBytes()
    {
        var source = new byte[] { 1, 2, 3, 4, 5 };
        var handler = new StaticBytesHandler(source);
        var engine = new HttpRangeDownloadEngine(new HttpClient(handler));
        var directory = Path.Combine(Path.GetTempPath(), "ilearn-download-tests", Guid.NewGuid().ToString("N"));
        var request = new DownloadRequest("task-1", "https://example.test/video.mp4", "video.mp4", directory, "第一讲", "HDMI");
        DownloadTaskSnapshot? last = null;

        await engine.DownloadAsync(
            request,
            Path.Combine(directory, request.FileName),
            new Progress<DownloadTaskSnapshot>(snapshot => last = snapshot),
            CancellationToken.None);

        Assert.Equal(source, await File.ReadAllBytesAsync(Path.Combine(directory, request.FileName)));
        Assert.NotNull(last);
        Assert.Equal(source.Length, last!.BytesReceived);
    }

    private sealed class StaticBytesHandler : HttpMessageHandler
    {
        private readonly byte[] _bytes;

        public StaticBytesHandler(byte[] bytes)
        {
            _bytes = bytes;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_bytes)
            };
            response.Content.Headers.ContentLength = _bytes.Length;
            return Task.FromResult(response);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~HttpRangeDownloadEngineTests
```

Expected: FAIL because `HttpRangeDownloadEngine` does not exist.

- [ ] **Step 3: Add HTTP engine**

Create `iLearn/Downloads/HttpRangeDownloadEngine.cs`:

```csharp
using System.Diagnostics;

namespace iLearn.Downloads;

public sealed class HttpRangeDownloadEngine : IDownloadEngine
{
    private readonly HttpClient _httpClient;

    public HttpRangeDownloadEngine()
        : this(new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
    {
    }

    public HttpRangeDownloadEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DownloadAsync(
        DownloadRequest request,
        string outputPath,
        IProgress<DownloadTaskSnapshot> progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var response = await _httpClient.GetAsync(
            request.Url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(outputPath);

        var buffer = new byte[128 * 1024];
        long bytesReceived = 0;
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesReceived += read;

            if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(250))
            {
                lastReport = stopwatch.Elapsed;
                progress.Report(DownloadTaskSnapshot.Downloading(
                    request,
                    bytesReceived,
                    totalBytes,
                    bytesReceived / Math.Max(1, stopwatch.Elapsed.TotalSeconds),
                    null));
            }
        }

        progress.Report(DownloadTaskSnapshot.Downloading(
            request,
            bytesReceived,
            totalBytes,
            bytesReceived / Math.Max(1, stopwatch.Elapsed.TotalSeconds),
            null));
    }
}
```

- [ ] **Step 4: Wire queue processing with concurrency**

Add these fields to `DownloadQueueService`:

```csharp
private readonly Channel<DownloadRequest> _channel = Channel.CreateUnbounded<DownloadRequest>();
private readonly SemaphoreSlim _concurrency = new(3, 3);
```

Add `using System.Threading.Channels;`.

In `EnqueueAsync`, after `Upsert(...)`, add:

```csharp
await _channel.Writer.WriteAsync(request, cancellationToken);
_ = Task.Run(ProcessQueueAsync, cancellationToken);
```

Add this method to `DownloadQueueService`:

```csharp
private async Task ProcessQueueAsync()
{
    while (_channel.Reader.TryRead(out var request))
    {
        await _concurrency.WaitAsync();
        _ = Task.Run(async () =>
        {
            var cts = new CancellationTokenSource();
            _cancellations[request.Id] = cts;
            try
            {
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Downloading));
                var outputPath = Path.Combine(request.OutputDirectory, request.FileName);
                await _engine.DownloadAsync(
                    request,
                    outputPath,
                    new Progress<DownloadTaskSnapshot>(Upsert),
                    cts.Token);
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Completed));
            }
            catch (OperationCanceledException)
            {
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Cancelled));
            }
            catch (Exception ex)
            {
                Upsert(DownloadTaskSnapshot.FromRequest(request, DownloadTaskStatus.Failed, ex.Message));
            }
            finally
            {
                _cancellations.Remove(request.Id);
                _concurrency.Release();
            }
        });
    }
}
```

- [ ] **Step 5: Register download services**

In `iLearn/Composition/ServiceCollectionExtensions.cs`, add:

```csharp
using iLearn.Downloads;
```

Register:

```csharp
services.AddSingleton<IDownloadEngine, HttpRangeDownloadEngine>();
services.AddSingleton<DownloadQueueService>();
```

Do not remove `VideoDownloadService` until Task 6 has migrated all callers.

- [ ] **Step 6: Run download tests**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~HttpRangeDownloadEngineTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add iLearn/Downloads iLearn/Composition/ServiceCollectionExtensions.cs iLearn.Tests/Downloads
git commit -m "feat: implement cross-platform download engine"
```

---

### Task 6: Rebuild Download Selection and Manager View Models

**Files:**
- Modify: `iLearn/ViewModels/Pages/VideoDownloadListViewModel.cs`
- Modify: `iLearn/ViewModels/Pages/DownloadManageViewModel.cs`
- Modify: `iLearn/ViewModels/Pages/SettingViewModel.cs`
- Create: `iLearn.Tests/Downloads/DownloadSelectionViewModelTests.cs`

- [ ] **Step 1: Write download selection test**

Create `iLearn.Tests/Downloads/DownloadSelectionViewModelTests.cs`:

```csharp
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
    public void SelectedDownloadText_ShowsHelpfulCount()
    {
        var videos = new List<LiveAndRecordInfo>
        {
            new() { ResourceId = "r1", LiveRecordName = "第一讲", TeacherName = "张老师", IsHdmiSelected = true },
            new() { ResourceId = "r2", LiveRecordName = "第二讲", TeacherName = "李老师", IsTeacherSelected = true }
        };

        var viewModel = new VideoDownloadListViewModel(
            videos,
            new DownloadQueueService(new FakeEngine()),
            new FakeApiService(),
            new NotificationService(),
            new AppConfig());

        Assert.Equal("已选择 2 个文件", viewModel.SelectedDownloadText);
    }

    private sealed class FakeEngine : IDownloadEngine
    {
        public Task DownloadAsync(DownloadRequest request, string outputPath, IProgress<DownloadTaskSnapshot> progress, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeApiService : ILearnApiService
    {
    }
}
```

- [ ] **Step 2: Make `ILearnApiService` mockable**

In `iLearn/Services/iLearnApiService.cs`, keep the public class and mark these methods `virtual` so tests can override network calls:

```csharp
public virtual Task<VideoInfo> GetVideoInfoAsync(string resourceId)
public virtual Task<List<TermInfo>> GetTermsAsync()
public virtual Task<List<ClassInfo>> GetClassesAsync(string year, string term)
public virtual Task<List<LiveAndRecordInfo>> GetLiveAndRecordInfoAsync(string termId, string classId)
```

Keep the existing method bodies unchanged.

- [ ] **Step 3: Run test to verify current constructor fails**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~DownloadSelectionViewModelTests
```

Expected: FAIL until constructor dependencies are updated from `VideoDownloadService`/WPF snackbar to `DownloadQueueService`/`INotificationService`.

- [ ] **Step 4: Replace `VideoDownloadListViewModel` dependencies**

In `iLearn/ViewModels/Pages/VideoDownloadListViewModel.cs`, replace WPF usings with:

```csharp
using iLearn.Downloads;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
```

Change constructor dependencies to:

```csharp
private readonly DownloadQueueService _downloadQueue;
private readonly ILearnApiService _iLearnApiService;
private readonly INotificationService _notifications;
private readonly AppConfig _appConfig;
```

Use this constructor:

```csharp
public VideoDownloadListViewModel(
    List<LiveAndRecordInfo> liveAndRecordInfos,
    DownloadQueueService downloadQueue,
    ILearnApiService iLearnApiService,
    INotificationService notifications,
    AppConfig appConfig)
{
    _downloadQueue = downloadQueue;
    _iLearnApiService = iLearnApiService;
    _notifications = notifications;
    _appConfig = appConfig;

    Videos = new ObservableCollection<LiveAndRecordInfo>(liveAndRecordInfos ?? []);

    foreach (var video in Videos)
        video.PropertyChanged += OnVideoPropertyChanged;

    RefreshSelectedDownloadCount();
}
```

Replace `ShowSnackbar` with:

```csharp
private void ShowNotification(string title, string message, AppNotificationKind kind)
{
    _notifications.Show(title, message, kind);
}
```

In `DownloadSelected`, replace final snackbar call with:

```csharp
ShowNotification(
    "下载任务已添加",
    $"成功加入队列 {queued} 个，失败 {failed} 个",
    failed == 0 ? AppNotificationKind.Success : AppNotificationKind.Warning);
```

Replace the empty-selection branch with:

```csharp
ShowNotification("请选择要下载的视频", "没有选中任何视频进行下载", AppNotificationKind.Info);
return;
```

Replace `DownloadVideoAsync` enqueue call with:

```csharp
var request = new DownloadRequest(
    Id: $"{video.ResourceId}-{perspective}",
    Url: videoSource.VideoPath,
    FileName: fileName,
    OutputDirectory: folder,
    DisplayName: video.LiveRecordName,
    Perspective: perspective);

await _downloadQueue.EnqueueAsync(request);
return true;
```

Replace subtitle enqueue call with:

```csharp
await _downloadQueue.EnqueueAsync(new DownloadRequest(
    Id: $"{videoInfo.LiveRecordId}-subtitle",
    Url: videoInfo.PhaseUrl,
    FileName: fileName,
    OutputDirectory: folder,
    DisplayName: $"{videoInfo.ResourceName} 字幕",
    Perspective: "字幕"));
```

- [ ] **Step 5: Rebuild download manager view model**

Replace `DownloadManageViewModel` WPF timer and snackbar dependencies with:

```csharp
using iLearn.Downloads;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Platform;
using System.Collections.ObjectModel;

namespace iLearn.ViewModels.Pages;

public partial class DownloadManageViewModel : ObservableObject
{
    private readonly DownloadQueueService _downloadQueue;
    private readonly INotificationService _notifications;
    private readonly IPlatformLauncher _launcher;
    private readonly AppConfig _appConfig;

    public DownloadManageViewModel(
        DownloadQueueService downloadQueue,
        INotificationService notifications,
        IPlatformLauncher launcher,
        AppConfig appConfig)
    {
        _downloadQueue = downloadQueue;
        _notifications = notifications;
        _launcher = launcher;
        _appConfig = appConfig;
        Downloads = _downloadQueue.Tasks;
    }

    public ReadOnlyObservableCollection<DownloadTaskSnapshot> Downloads { get; }

    public int ActiveDownloadsCount => Downloads.Count(d => d.Status == DownloadTaskStatus.Downloading);
    public int CompletedDownloadsCount => Downloads.Count(d => d.Status == DownloadTaskStatus.Completed);
    public int QueuedDownloadsCount => Downloads.Count(d => d.Status is DownloadTaskStatus.Queued or DownloadTaskStatus.Waiting);
    public string TotalDownloadSpeed => FormatBytesPerSecond(Downloads.Where(d => d.Status == DownloadTaskStatus.Downloading).Sum(d => d.BytesPerSecond));

    [RelayCommand]
    private async Task PauseDownload(DownloadTaskSnapshot item)
    {
        await _downloadQueue.PauseAsync(item.Id);
        _notifications.Show("下载已暂停", item.FileName, AppNotificationKind.Info);
    }

    [RelayCommand]
    private async Task RetryDownload(DownloadTaskSnapshot item)
    {
        await _downloadQueue.RetryAsync(item.Id);
        _notifications.Show("重试开始", item.FileName, AppNotificationKind.Info);
    }

    [RelayCommand]
    private async Task CancelDownload(DownloadTaskSnapshot item)
    {
        await _downloadQueue.CancelAsync(item.Id);
        _notifications.Show("下载已取消", item.FileName, AppNotificationKind.Warning);
    }

    [RelayCommand]
    private async Task OpenDownloadFile(DownloadTaskSnapshot item)
    {
        await _launcher.OpenFileAsync(item.OutputPath);
    }

    [RelayCommand]
    private async Task OpenDownloadsFolder()
    {
        await _launcher.OpenFolderAsync(_appConfig.DownloadPath);
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{bytesPerSecond / 1024 / 1024:0.##} MB/s";
        if (bytesPerSecond >= 1024)
            return $"{bytesPerSecond / 1024:0.##} KB/s";
        return $"{bytesPerSecond:0} B/s";
    }
}
```

- [ ] **Step 6: Update settings view model download controls**

In `SettingViewModel`, remove `Wpf.Ui` and `Microsoft.Win32` usings. Inject `DownloadQueueService`, `IPlatformLauncher`, and `INotificationService`.

Replace folder browsing with an Avalonia storage provider command in the `SettingPage.axaml.cs` task. Keep this view model command for opening folders:

```csharp
[RelayCommand]
private async Task OpenDownloadPath()
{
    await _launcher.OpenFolderAsync(DownloadPath);
}
```

For settings changes, show immediate feedback:

```csharp
private void SaveDownloadSettings(string message)
{
    _appConfig.Save();
    _notifications.Show("设置已保存", message, AppNotificationKind.Success);
}
```

- [ ] **Step 7: Run download view model tests**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~DownloadSelectionViewModelTests
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add iLearn/ViewModels/Pages/VideoDownloadListViewModel.cs iLearn/ViewModels/Pages/DownloadManageViewModel.cs iLearn/ViewModels/Pages/SettingViewModel.cs iLearn/Services/iLearnApiService.cs iLearn.Tests/Downloads
git commit -m "feat: migrate download view models to avalonia services"
```

---

### Task 7: Build Avalonia Shell and Login Flow with Visible Feedback

**Files:**
- Create: `iLearn/ViewModels/AppViewModelBase.cs`
- Create: `iLearn/ViewModels/ShellViewModel.cs`
- Modify: `iLearn/ViewModels/Windows/LoginViewModel.cs`
- Create: `iLearn/Views/Windows/LoginWindow.axaml`
- Create: `iLearn/Views/Windows/LoginWindow.axaml.cs`
- Create: `iLearn/Views/Windows/MainWindow.axaml`
- Create: `iLearn/Views/Windows/MainWindow.axaml.cs`

- [ ] **Step 1: Add view model base**

Create `iLearn/ViewModels/AppViewModelBase.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace iLearn.ViewModels;

public abstract partial class AppViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = string.Empty;

    protected void BeginBusy(string statusText)
    {
        StatusText = statusText;
        IsBusy = true;
    }

    protected void EndBusy(string statusText = "")
    {
        StatusText = statusText;
        IsBusy = false;
    }
}
```

- [ ] **Step 2: Add shell view model**

Create `iLearn/ViewModels/ShellViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iLearn.Navigation;
using iLearn.Notifications;
using System.Collections.ObjectModel;

namespace iLearn.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly NavigationService _navigation;

    public ShellViewModel(NavigationService navigation, INotificationService notifications)
    {
        _navigation = navigation;
        Notifications = notifications;
        Items =
        [
            new NavigationItemViewModel(AppRoute.Courses, "我的课程", "Home"),
            new NavigationItemViewModel(AppRoute.Media, "课程视频", "Video"),
            new NavigationItemViewModel(AppRoute.DownloadSelection, "课程下载", "Download"),
            new NavigationItemViewModel(AppRoute.Downloads, "下载管理", "ListChecks"),
            new NavigationItemViewModel(AppRoute.LocalVideos, "本地视频", "FolderVideo"),
            new NavigationItemViewModel(AppRoute.Settings, "设置", "Settings")
        ];
        SelectRoute(AppRoute.Courses);
    }

    public ObservableCollection<NavigationItemViewModel> Items { get; }
    public INotificationService Notifications { get; }

    [ObservableProperty]
    private AppRoute _currentRoute = AppRoute.Courses;

    [RelayCommand]
    private void Navigate(NavigationItemViewModel item)
    {
        SelectRoute(item.Route);
        _navigation.NavigateTo(item.Route);
    }

    private void SelectRoute(AppRoute route)
    {
        CurrentRoute = route;
        foreach (var item in Items)
            item.IsSelected = item.Route == route;
    }
}
```

- [ ] **Step 3: Migrate login view model dependencies**

In `LoginViewModel`, remove `System.Windows.*`, `Wpf.Ui.*`, `WindowsManagerService`, and `IContentDialogService` dependencies.

Add dependencies:

```csharp
private readonly INotificationService _notifications;
private readonly ISecretStore _secretStore;
private readonly IServiceProvider _services;
```

Use Avalonia image bytes instead of `BitmapImage`:

```csharp
[ObservableProperty]
private byte[]? _captchaImageBytes;
```

Update refresh method:

```csharp
[RelayCommand]
private async Task RefreshCasCaptchaAsync()
{
    try
    {
        CaptchaImageBytes = await _learnApiService.GetCasCaptchaBytesAsync();
    }
    catch
    {
        _notifications.Show("验证码加载失败", "请检查网络后重试", AppNotificationKind.Warning);
    }
}
```

Replace `ShowSnackbar` calls with:

```csharp
private void ShowMessage(string title, string message, AppNotificationKind kind)
{
    _notifications.Show(title, message, kind);
}
```

When login starts:

```csharp
IsAuthenticationInProgress = true;
ShowMessage("正在登录", "正在连接统一认证服务", AppNotificationKind.Info);
```

On success:

```csharp
private async void OnLoginSuccess()
{
    if (IsRememberMeEnabled)
    {
        _appConfig.UserName = UserName;
        await _secretStore.SaveSecretAsync("login-password", UserPassword);
    }
    else
    {
        _appConfig.UserName = null;
        await _secretStore.DeleteSecretAsync("login-password");
    }

    _appConfig.IsRememberMeEnabled = IsRememberMeEnabled;
    _appConfig.IsAutoLoginEnabled = IsAutoLoginEnabled;
    _appConfig.Save();

    var mainWindow = _services.GetRequiredService<MainWindow>();
    mainWindow.Show();
    App.Current?.ApplicationLifetime
        ?.GetType()
        .GetProperty("MainWindow")
        ?.SetValue(App.Current.ApplicationLifetime, mainWindow);
}
```

- [ ] **Step 4: Add login window XAML**

Create `iLearn/Views/Windows/LoginWindow.axaml`:

```xml
<Window
    x:Class="iLearn.Views.Windows.LoginWindow"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Windows"
    x:DataType="vm:LoginViewModel"
    Width="920"
    Height="620"
    MinWidth="760"
    MinHeight="560"
    Title="iLearn">
  <Grid ColumnDefinitions="1.1*,0.9*" Background="#F5F7FB">
    <Border Grid.Column="0" Padding="48" Background="#111827">
      <StackPanel VerticalAlignment="Center" Spacing="18">
        <TextBlock Text="iLearn" FontSize="42" FontWeight="Bold" Foreground="White" />
        <TextBlock Text="学在吉大桌面客户端" FontSize="20" Foreground="#CBD5E1" />
        <TextBlock Text="课程、视频、下载和本地学习都在一个更顺手的客户端里。"
                   TextWrapping="Wrap"
                   Foreground="#94A3B8"
                   FontSize="15" />
      </StackPanel>
    </Border>

    <Border Grid.Column="1" Padding="42" Background="White">
      <StackPanel VerticalAlignment="Center" Spacing="16">
        <TextBlock Text="登录" FontSize="28" FontWeight="SemiBold" Foreground="#111827" />
        <TextBox Watermark="学号 / 用户名" Text="{Binding UserName}" />
        <TextBox Watermark="密码" Text="{Binding UserPassword}" PasswordChar="*" />
        <CheckBox Content="记住密码" IsChecked="{Binding IsRememberMeEnabled}" />
        <CheckBox Content="下次自动登录" IsChecked="{Binding IsAutoLoginEnabled}" />
        <Button Content="验证并登录"
                Command="{Binding LoginCommand}"
                IsEnabled="{Binding !IsAuthenticationInProgress}" />
        <ProgressBar IsIndeterminate="True" IsVisible="{Binding IsAuthenticationInProgress}" />
        <TextBlock Text="{Binding StatusText}" Foreground="#64748B" TextWrapping="Wrap" />
      </StackPanel>
    </Border>
  </Grid>
</Window>
```

- [ ] **Step 5: Add login code-behind**

Create `iLearn/Views/Windows/LoginWindow.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Windows;

namespace iLearn.Views.Windows;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 6: Add main shell window**

Create `iLearn/Views/Windows/MainWindow.axaml`:

```xml
<Window
    x:Class="iLearn.Views.Windows.MainWindow"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels"
    x:DataType="vm:ShellViewModel"
    Width="1180"
    Height="760"
    MinWidth="980"
    MinHeight="640"
    Title="iLearn">
  <Grid ColumnDefinitions="236,*" Background="#F6F8FB">
    <Border Grid.Column="0" Background="#111827" Padding="18">
      <DockPanel>
        <TextBlock DockPanel.Dock="Top"
                   Text="iLearn"
                   FontSize="28"
                   FontWeight="Bold"
                   Foreground="White"
                   Margin="0,8,0,22" />
        <ItemsControl ItemsSource="{Binding Items}">
          <ItemsControl.ItemTemplate>
            <DataTemplate>
              <Button Command="{Binding DataContext.NavigateCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                      CommandParameter="{Binding}"
                      HorizontalAlignment="Stretch"
                      Margin="0,0,0,8"
                      Content="{Binding Title}" />
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </DockPanel>
    </Border>

    <Grid Grid.Column="1" RowDefinitions="*,Auto">
      <ContentControl Name="PageHost" Margin="28" />
      <ItemsControl Grid.Row="1" ItemsSource="{Binding Notifications.Items}" Margin="28,0,28,18">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Border Background="#FFFFFF" BorderBrush="#E5E7EB" BorderThickness="1" Padding="12" Margin="0,0,0,8">
              <StackPanel>
                <TextBlock Text="{Binding Title}" FontWeight="SemiBold" />
                <TextBlock Text="{Binding Message}" Foreground="#64748B" TextWrapping="Wrap" />
              </StackPanel>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </Grid>
  </Grid>
</Window>
```

Create `iLearn/Views/Windows/MainWindow.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.Navigation;
using iLearn.ViewModels;
using iLearn.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Views.Windows;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(ShellViewModel viewModel, NavigationService navigation, IServiceProvider services)
    {
        InitializeComponent();
        DataContext = viewModel;
        _services = services;
        navigation.RouteChanged += (_, route) => ShowRoute(route);
        ShowRoute(AppRoute.Courses);
    }

    private void ShowRoute(AppRoute route)
    {
        PageHost.Content = route switch
        {
            AppRoute.Courses => _services.GetRequiredService<CoursesPage>(),
            AppRoute.Media => _services.GetRequiredService<MediaPage>(),
            AppRoute.DownloadSelection => _services.GetRequiredService<VideoDownloadListPage>(),
            AppRoute.Downloads => _services.GetRequiredService<DownloadManagePage>(),
            AppRoute.LocalVideos => _services.GetRequiredService<LocalVideoPage>(),
            AppRoute.Settings => _services.GetRequiredService<SettingPage>(),
            _ => _services.GetRequiredService<CoursesPage>()
        };
    }
}
```

- [ ] **Step 7: Run build**

Run:

```bash
dotnet build iLearn.sln
```

Expected: build still fails until page views are migrated in Task 8, but there should be no errors in `Program.cs`, `App.axaml.cs`, `LoginWindow`, or `MainWindow` files.

- [ ] **Step 8: Commit**

```bash
git add iLearn/ViewModels/AppViewModelBase.cs iLearn/ViewModels/ShellViewModel.cs iLearn/ViewModels/Windows/LoginViewModel.cs iLearn/Views/Windows
git commit -m "feat: add avalonia shell and login feedback"
```

---

### Task 8: Migrate Core Pages to Avalonia

**Files:**
- Modify: `iLearn/ViewModels/Pages/CoursesViewModel.cs`
- Modify: `iLearn/ViewModels/Pages/MediaViewModel.cs`
- Modify: `iLearn/ViewModels/Pages/LocalVideoViewModel.cs`
- Create/Replace: `iLearn/Views/Pages/*.axaml`
- Create/Replace: `iLearn/Views/Pages/*.axaml.cs`

- [ ] **Step 1: Remove WPF navigation dependencies from courses**

In `CoursesViewModel`, replace WPF navigation fields with:

```csharp
private readonly NavigationService _navigationService;
private readonly INotificationService _notifications;
```

Use this course selection command:

```csharp
[RelayCommand]
private void CourseSelected(ClassInfo course)
{
    if (course is null)
    {
        _notifications.Show("无法打开课程", "课程数据为空，请刷新后重试", AppNotificationKind.Warning);
        return;
    }

    _navigationService.NavigateTo(AppRoute.Media);
    WeakReferenceMessenger.Default.Send(new CourseMessage { classInfo = course });
}
```

- [ ] **Step 2: Add Courses page**

Create `iLearn/Views/Pages/CoursesPage.axaml`:

```xml
<UserControl
    x:Class="iLearn.Views.Pages.CoursesPage"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Pages"
    x:DataType="vm:CoursesViewModel">
  <Grid RowDefinitions="Auto,*" RowSpacing="18">
    <DockPanel>
      <StackPanel>
        <TextBlock Text="我的课程" FontSize="30" FontWeight="SemiBold" />
        <TextBlock Text="选择学期后进入课程视频" Foreground="#64748B" />
      </StackPanel>
      <ComboBox DockPanel.Dock="Right"
                Width="260"
                ItemsSource="{Binding TermsOptions}"
                SelectedItem="{Binding SelectedTerm}" />
    </DockPanel>
    <ItemsControl Grid.Row="1" ItemsSource="{Binding MyCourses}">
      <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
          <WrapPanel />
        </ItemsPanelTemplate>
      </ItemsControl.ItemsPanel>
      <ItemsControl.ItemTemplate>
        <DataTemplate>
          <Button Command="{Binding DataContext.CourseSelectedCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                  CommandParameter="{Binding}"
                  Width="280"
                  Height="138"
                  Margin="0,0,16,16">
            <StackPanel HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
              <TextBlock Text="{Binding CourseName}" FontSize="18" FontWeight="SemiBold" TextWrapping="Wrap" />
              <TextBlock Text="{Binding TeacherName}" Foreground="#64748B" Margin="0,8,0,0" />
              <TextBlock Text="{Binding StatusName}" Foreground="#0F766E" Margin="0,12,0,0" />
            </StackPanel>
          </Button>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>
  </Grid>
</UserControl>
```

Create `iLearn/Views/Pages/CoursesPage.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class CoursesPage : UserControl
{
    public CoursesPage(CoursesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 3: Update media page view model feedback**

In `MediaViewModel`, remove `Wpf.Ui` dependencies and inject:

```csharp
private readonly NavigationService _navigationService;
private readonly INotificationService _notifications;
private readonly IPlatformLauncher _launcher;
```

Replace `OpenMediaAsync` notification and launch code with:

```csharp
[RelayCommand]
private async Task OpenMediaAsync(LiveAndRecordInfo media)
{
    if (media.ResourceId is null)
    {
        _notifications.Show("无法播放", "该资源没有视频可供播放", AppNotificationKind.Warning);
        return;
    }

    _notifications.Show("正在加载视频", "正在获取视频地址和字幕", AppNotificationKind.Info);
    var videoInfo = await _ilearnApiService.GetVideoInfoAsync(media.ResourceId);
    var playerPath = Path.Combine(AppContext.BaseDirectory, "Assets", "VideoPlayer.html");
    var content = await File.ReadAllTextAsync(playerPath);

    content = content
        .Replace("_LEFTVIDEO_", videoInfo.VideoList.ElementAtOrDefault(0)?.VideoPath ?? "")
        .Replace("_RIGHTVIDEO_", videoInfo.VideoList.ElementAtOrDefault(1)?.VideoPath ?? "")
        .Replace("_SUBTITLE_", videoInfo.PhaseUrl ?? "");

    var tempFile = Path.Combine(Path.GetTempPath(), $"video_preview_{Guid.NewGuid():N}.html");
    await File.WriteAllTextAsync(tempFile, content);
    await _launcher.OpenFileAsync(tempFile);
}
```

- [ ] **Step 4: Add Media page**

Create `iLearn/Views/Pages/MediaPage.axaml`:

```xml
<UserControl
    x:Class="iLearn.Views.Pages.MediaPage"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Pages"
    x:DataType="vm:MediaViewModel">
  <Grid RowDefinitions="Auto,*" RowSpacing="18">
    <DockPanel>
      <StackPanel>
        <TextBlock Text="课程视频" FontSize="30" FontWeight="SemiBold" />
        <TextBlock Text="{Binding CurrentCourse.CourseName}" Foreground="#64748B" />
      </StackPanel>
      <TextBox DockPanel.Dock="Right" Width="300" Watermark="搜索视频或教师" Text="{Binding SearchQuery}" />
    </DockPanel>
    <ListBox Grid.Row="1" ItemsSource="{Binding MediaItems}">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <Grid ColumnDefinitions="*,Auto" Margin="0,0,0,10">
            <StackPanel>
              <TextBlock Text="{Binding LiveRecordName}" FontSize="17" FontWeight="SemiBold" />
              <TextBlock Text="{Binding TeacherName}" Foreground="#64748B" />
              <TextBlock Text="{Binding TimeRange}" Foreground="#94A3B8" />
            </StackPanel>
            <Button Grid.Column="1"
                    Content="播放"
                    Command="{Binding DataContext.OpenMediaCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                    CommandParameter="{Binding}" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</UserControl>
```

Create `iLearn/Views/Pages/MediaPage.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class MediaPage : UserControl
{
    public MediaPage(MediaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 5: Add download selection page**

Create `iLearn/Views/Pages/VideoDownloadListPage.axaml`:

```xml
<UserControl
    x:Class="iLearn.Views.Pages.VideoDownloadListPage"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Pages"
    x:DataType="vm:VideoDownloadListViewModel">
  <Grid RowDefinitions="Auto,Auto,*" RowSpacing="14">
    <DockPanel>
      <StackPanel>
        <TextBlock Text="课程下载" FontSize="30" FontWeight="SemiBold" />
        <TextBlock Text="{Binding SelectedDownloadText}" Foreground="#64748B" />
      </StackPanel>
      <Button DockPanel.Dock="Right"
              Content="加入下载队列"
              Command="{Binding DownloadSelectedCommand}" />
    </DockPanel>
    <Grid Grid.Row="1" ColumnDefinitions="*,Auto,Auto" ColumnSpacing="12">
      <TextBox Watermark="搜索视频或教师" Text="{Binding SearchText}" />
      <CheckBox Grid.Column="1" Content="全选 HDMI" IsChecked="{Binding IsAllHdmiSelected}" />
      <CheckBox Grid.Column="2" Content="全选教师" IsChecked="{Binding IsAllTeacherSelected}" />
    </Grid>
    <ListBox Grid.Row="2" ItemsSource="{Binding FilteredVideos}">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <Grid ColumnDefinitions="*,Auto,Auto" Margin="0,0,0,10">
            <StackPanel>
              <TextBlock Text="{Binding LiveRecordName}" FontWeight="SemiBold" />
              <TextBlock Text="{Binding TeacherName}" Foreground="#64748B" />
            </StackPanel>
            <CheckBox Grid.Column="1" Content="HDMI" IsChecked="{Binding IsHdmiSelected}" />
            <CheckBox Grid.Column="2" Content="教师" IsChecked="{Binding IsTeacherSelected}" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</UserControl>
```

Create `iLearn/Views/Pages/VideoDownloadListPage.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class VideoDownloadListPage : UserControl
{
    public VideoDownloadListPage(VideoDownloadListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 6: Add download manager page**

Create `iLearn/Views/Pages/DownloadManagePage.axaml`:

```xml
<UserControl
    x:Class="iLearn.Views.Pages.DownloadManagePage"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Pages"
    x:DataType="vm:DownloadManageViewModel">
  <Grid RowDefinitions="Auto,Auto,*" RowSpacing="14">
    <DockPanel>
      <StackPanel>
        <TextBlock Text="下载管理" FontSize="30" FontWeight="SemiBold" />
        <TextBlock Text="{Binding TotalDownloadSpeed}" Foreground="#64748B" />
      </StackPanel>
      <Button DockPanel.Dock="Right" Content="打开下载文件夹" Command="{Binding OpenDownloadsFolderCommand}" />
    </DockPanel>
    <UniformGrid Grid.Row="1" Columns="3">
      <TextBlock Text="{Binding ActiveDownloadsCount, StringFormat='下载中 {0}'}" />
      <TextBlock Text="{Binding QueuedDownloadsCount, StringFormat='排队 {0}'}" />
      <TextBlock Text="{Binding CompletedDownloadsCount, StringFormat='已完成 {0}'}" />
    </UniformGrid>
    <ListBox Grid.Row="2" ItemsSource="{Binding Downloads}">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <Grid ColumnDefinitions="*,Auto,Auto,Auto" Margin="0,0,0,12">
            <StackPanel>
              <TextBlock Text="{Binding FileName}" FontWeight="SemiBold" />
              <ProgressBar Minimum="0" Maximum="100" Value="{Binding Progress}" />
              <TextBlock Text="{Binding ErrorMessage}" Foreground="#DC2626" />
            </StackPanel>
            <Button Grid.Column="1" Content="暂停" Command="{Binding DataContext.PauseDownloadCommand, RelativeSource={RelativeSource AncestorType=UserControl}}" CommandParameter="{Binding}" />
            <Button Grid.Column="2" Content="重试" Command="{Binding DataContext.RetryDownloadCommand, RelativeSource={RelativeSource AncestorType=UserControl}}" CommandParameter="{Binding}" />
            <Button Grid.Column="3" Content="取消" Command="{Binding DataContext.CancelDownloadCommand, RelativeSource={RelativeSource AncestorType=UserControl}}" CommandParameter="{Binding}" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</UserControl>
```

Create `iLearn/Views/Pages/DownloadManagePage.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class DownloadManagePage : UserControl
{
    public DownloadManagePage(DownloadManageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 7: Add local videos and settings pages**

Before creating `LocalVideoPage.axaml`, migrate `LocalVideoViewModel` away from WPF:

```csharp
private readonly INotificationService _notifications;
private readonly IPlatformLauncher _launcher;
private readonly AppConfig _appConfig;
```

Use this constructor:

```csharp
public LocalVideoViewModel(
    INotificationService notifications,
    IPlatformLauncher launcher,
    AppConfig appConfig)
{
    _notifications = notifications;
    _launcher = launcher;
    _appConfig = appConfig;
    _ = LoadLocalVideos();
}
```

Replace `ShowSnackbar` with:

```csharp
private void ShowNotification(string title, string message, AppNotificationKind kind)
{
    _notifications.Show(title, message, kind);
}
```

Replace `OpenFileLocation` with:

```csharp
[RelayCommand]
private async Task OpenFileLocation(LocalVideoFile video)
{
    if (video == null || !File.Exists(video.FullPath))
    {
        ShowNotification("文件不存在", "视频文件已被移动或删除", AppNotificationKind.Error);
        return;
    }

    var directory = Path.GetDirectoryName(video.FullPath);
    if (!string.IsNullOrWhiteSpace(directory))
        await _launcher.OpenFolderAsync(directory);
}
```

Replace `OpenLocalVideoAsync` resource loading with:

```csharp
var playerPath = Path.Combine(AppContext.BaseDirectory, "Assets", "VideoPlayer.html");
var content = await File.ReadAllTextAsync(playerPath);
```

Create `iLearn/Views/Pages/LocalVideoPage.axaml`:

```xml
<UserControl
    x:Class="iLearn.Views.Pages.LocalVideoPage"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Pages"
    x:DataType="vm:LocalVideoViewModel">
  <Grid RowDefinitions="Auto,Auto,*" RowSpacing="14">
    <DockPanel>
      <StackPanel>
        <TextBlock Text="本地视频" FontSize="30" FontWeight="SemiBold" />
        <TextBlock Text="自动识别下载目录中的课程视频和字幕" Foreground="#64748B" />
      </StackPanel>
      <Button DockPanel.Dock="Right" Content="刷新" Command="{Binding LoadLocalVideosCommand}" />
    </DockPanel>
    <Grid Grid.Row="1" ColumnDefinitions="*,180" ColumnSpacing="12">
      <TextBox Watermark="搜索课程或文件名" Text="{Binding SearchQuery}" />
      <ComboBox Grid.Column="1" ItemsSource="{Binding FilterOptions}" SelectedItem="{Binding SelectedFilter}" />
    </Grid>
    <ListBox Grid.Row="2" ItemsSource="{Binding FilteredVideos}">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <Grid ColumnDefinitions="*,Auto,Auto" Margin="0,0,0,12">
            <StackPanel>
              <TextBlock Text="{Binding CourseName}" FontWeight="SemiBold" />
              <TextBlock Text="{Binding FileName}" Foreground="#64748B" />
              <TextBlock Text="{Binding FileSizeFormatted}" Foreground="#94A3B8" />
            </StackPanel>
            <Button Grid.Column="1"
                    Content="播放"
                    Command="{Binding DataContext.OpenVideoCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                    CommandParameter="{Binding}" />
            <Button Grid.Column="2"
                    Content="位置"
                    Command="{Binding DataContext.OpenFileLocationCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                    CommandParameter="{Binding}" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</UserControl>
```

Create `iLearn/Views/Pages/LocalVideoPage.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class LocalVideoPage : UserControl
{
    public LocalVideoPage(LocalVideoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

Create `iLearn/Views/Pages/SettingPage.axaml`:

```xml
<UserControl
    x:Class="iLearn.Views.Pages.SettingPage"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:iLearn.ViewModels.Pages"
    x:DataType="vm:SettingViewModel">
  <ScrollViewer>
    <StackPanel Spacing="18">
      <StackPanel>
        <TextBlock Text="设置" FontSize="30" FontWeight="SemiBold" />
        <TextBlock Text="下载、更新和应用偏好" Foreground="#64748B" />
      </StackPanel>

      <Border Background="#FFFFFF" BorderBrush="#E5E7EB" BorderThickness="1" Padding="18">
        <StackPanel Spacing="14">
          <TextBlock Text="下载设置" FontSize="18" FontWeight="SemiBold" />
          <TextBlock Text="同时下载数" Foreground="#64748B" />
          <NumericUpDown Minimum="1" Maximum="10" Value="{Binding MaxConcurrentDownloads}" />
          <TextBlock Text="分块数" Foreground="#64748B" />
          <NumericUpDown Minimum="1" Maximum="32" Value="{Binding ChunkCount}" />
          <TextBlock Text="限速 MB/s，0 表示不限速" Foreground="#64748B" />
          <NumericUpDown Minimum="0" Value="{Binding SpeedLimitMBps}" />
          <TextBlock Text="下载目录" Foreground="#64748B" />
          <Grid ColumnDefinitions="*,Auto" ColumnSpacing="10">
            <TextBox Text="{Binding DownloadPath}" />
            <Button Grid.Column="1" Content="打开" Command="{Binding OpenDownloadPathCommand}" />
          </Grid>
          <Button Content="恢复下载默认设置" Command="{Binding ResetDownloadSettingsCommand}" />
        </StackPanel>
      </Border>

      <Border Background="#FFFFFF" BorderBrush="#E5E7EB" BorderThickness="1" Padding="18">
        <StackPanel Spacing="12">
          <TextBlock Text="更新" FontSize="18" FontWeight="SemiBold" />
          <TextBlock Text="{Binding AppVersion, StringFormat='当前版本 {0}'}" Foreground="#64748B" />
          <TextBlock Text="{Binding LastChecked, StringFormat='上次检查 {0}'}" Foreground="#64748B" />
          <Button Content="检查更新" Command="{Binding CheckForUpdatesCommand}" />
        </StackPanel>
      </Border>
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

Create `iLearn/Views/Pages/SettingPage.axaml.cs`:

```csharp
using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class SettingPage : UserControl
{
    public SettingPage(SettingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 8: Remove old WPF XAML files after pages compile**

Delete only after all `.axaml` replacements exist:

```bash
rm iLearn/Views/Pages/*.xaml iLearn/Views/Windows/*.xaml
```

- [ ] **Step 9: Run build**

Run:

```bash
dotnet build iLearn.sln
```

Expected: PASS or only errors from obsolete WPF usings in view models; fix those usings before continuing.

- [ ] **Step 10: Commit**

```bash
git add iLearn/ViewModels/Pages iLearn/Views/Pages
git add -u iLearn/Views
git commit -m "feat: migrate primary pages to avalonia"
```

---

### Task 9: Replace AutoUpdater.NET with Cross-Platform Manifest Updates

**Files:**
- Create: `iLearn/Updates/UpdateManifest.cs`
- Create: `iLearn/Updates/UpdateCheckResult.cs`
- Create: `iLearn/Updates/IUpdateService.cs`
- Create: `iLearn/Updates/UpdateService.cs`
- Modify: `iLearn/ViewModels/Pages/SettingViewModel.cs`
- Create: `iLearn.Tests/Updates/UpdateServiceTests.cs`

- [ ] **Step 1: Write update parsing test**

Create `iLearn.Tests/Updates/UpdateServiceTests.cs`:

```csharp
using iLearn.Updates;
using Xunit;

namespace iLearn.Tests.Updates;

public sealed class UpdateServiceTests
{
    [Fact]
    public async Task CheckAsync_ReturnsAvailable_WhenRemoteVersionIsNewer()
    {
        var json = """
        {
          "version": "9.0.0",
          "notes": "New release",
          "downloads": {
            "win-x64": "https://example.test/iLearn-win-x64.zip",
            "osx-arm64": "https://example.test/iLearn-osx-arm64.zip",
            "linux-x64": "https://example.test/iLearn-linux-x64.tar.gz"
          }
        }
        """;
        var service = new UpdateService(new StaticJsonClient(json), new Version(1, 3, 0));

        var result = await service.CheckAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("9.0.0", result.LatestVersion.ToString());
    }

    private sealed class StaticJsonClient : IUpdateManifestClient
    {
        private readonly string _json;

        public StaticJsonClient(string json)
        {
            _json = json;
        }

        public Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_json);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~UpdateServiceTests
```

Expected: FAIL because update types do not exist.

- [ ] **Step 3: Add update DTOs and service**

Create `iLearn/Updates/UpdateManifest.cs`:

```csharp
namespace iLearn.Updates;

public sealed class UpdateManifest
{
    public string Version { get; set; } = "0.0.0";
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, string> Downloads { get; set; } = new();
}
```

Create `iLearn/Updates/UpdateCheckResult.cs`:

```csharp
namespace iLearn.Updates;

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    Version LatestVersion,
    string Notes,
    string? DownloadUrl);
```

Create `iLearn/Updates/IUpdateService.cs`:

```csharp
namespace iLearn.Updates;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

public interface IUpdateManifestClient
{
    Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default);
}
```

Create `iLearn/Updates/UpdateService.cs`:

```csharp
using System.Runtime.InteropServices;
using System.Text.Json;

namespace iLearn.Updates;

public sealed class UpdateService : IUpdateService
{
    private readonly IUpdateManifestClient _client;
    private readonly Version _currentVersion;

    public UpdateService(IUpdateManifestClient client, Version currentVersion)
    {
        _client = client;
        _currentVersion = currentVersion;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var json = await _client.GetManifestJsonAsync(cancellationToken);
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new UpdateManifest();

        var latest = Version.Parse(manifest.Version);
        var rid = GetRuntimeId();
        manifest.Downloads.TryGetValue(rid, out var downloadUrl);

        return new UpdateCheckResult(latest > _currentVersion, latest, manifest.Notes, downloadUrl);
    }

    private static string GetRuntimeId()
    {
        var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"win-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"osx-{arch}";
        return $"linux-{arch}";
    }
}
```

- [ ] **Step 4: Add HTTP manifest client**

Append to `UpdateService.cs`:

```csharp
public sealed class HttpUpdateManifestClient : IUpdateManifestClient
{
    private readonly HttpClient _httpClient;
    private readonly string _manifestUrl;

    public HttpUpdateManifestClient(HttpClient httpClient, string manifestUrl)
    {
        _httpClient = httpClient;
        _manifestUrl = manifestUrl;
    }

    public async Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetStringAsync(_manifestUrl, cancellationToken);
    }
}
```

- [ ] **Step 5: Register update service**

In `ServiceCollectionExtensions`, register:

```csharp
services.AddSingleton<IUpdateManifestClient>(_ => new HttpUpdateManifestClient(
    new HttpClient(),
    "https://raw.githubusercontent.com/wzyyyyyyy/iLearn/refs/heads/master/iLearn/Assets/update-manifest.json"));
services.AddSingleton<IUpdateService>(sp => new UpdateService(
    sp.GetRequiredService<IUpdateManifestClient>(),
    typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0)));
```

- [ ] **Step 6: Replace settings update command**

In `SettingViewModel`, replace `CheckForUpdates` with:

```csharp
[RelayCommand]
private async Task CheckForUpdates()
{
    LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    var result = await _updateService.CheckAsync();
    if (!result.IsUpdateAvailable)
    {
        _notifications.Show("已是最新版本", $"当前版本 {AppVersion}", AppNotificationKind.Success);
        return;
    }

    _notifications.Show(
        "发现新版本",
        result.DownloadUrl is null
            ? $"最新版本 {result.LatestVersion}，但当前平台暂无下载包"
            : $"最新版本 {result.LatestVersion}，请打开下载链接更新",
        AppNotificationKind.Info);

    if (result.DownloadUrl is not null)
        await _launcher.OpenUrlAsync(result.DownloadUrl);
}
```

- [ ] **Step 7: Run update tests**

Run:

```bash
dotnet test iLearn.Tests/iLearn.Tests.csproj --filter FullyQualifiedName~UpdateServiceTests
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add iLearn/Updates iLearn/Composition/ServiceCollectionExtensions.cs iLearn/ViewModels/Pages/SettingViewModel.cs iLearn.Tests/Updates
git commit -m "feat: add cross-platform update checks"
```

---

### Task 10: Remove WPF-Only Code and Fix Build

**Files:**
- Delete: `iLearn/Services/WindowsManagerService.cs`
- Delete: `iLearn/Behaviors/MouseWheelToScrollBehavior.cs`
- Delete: `iLearn/Services/UpdateService.cs`
- Modify: all `iLearn/**/*.cs` files with `System.Windows`, `Wpf.Ui`, `Microsoft.Win32`, or `AutoUpdaterDotNET` usings
- Modify: `iLearn/Usings.cs`

- [ ] **Step 1: Find remaining WPF references**

Run:

```bash
rg -n "System\\.Windows|Wpf\\.Ui|Microsoft\\.Win32|AutoUpdaterDotNET|DispatcherTimer|BitmapImage|pack://application|explorer\\.exe|UseWPF|net10\\.0-windows" iLearn iLearn.Tests
```

Expected: output lists every remaining Windows/WPF-only reference.

- [ ] **Step 2: Remove obsolete files**

Run:

```bash
rm -f iLearn/Services/WindowsManagerService.cs iLearn/Behaviors/MouseWheelToScrollBehavior.cs iLearn/Services/UpdateService.cs
```

- [ ] **Step 3: Fix global usings**

Replace `iLearn/Usings.cs` with:

```csharp
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
```

- [ ] **Step 4: Build and fix compile errors**

Run:

```bash
dotnet build iLearn.sln
```

Expected: FAIL only if a referenced WPF type remains. For each compiler error:
- remove the WPF using,
- replace snackbar/dialog calls with `INotificationService`,
- replace file/folder launching with `IPlatformLauncher`,
- replace WPF image types with byte arrays or Avalonia `Bitmap`.

- [ ] **Step 5: Repeat WPF scan**

Run:

```bash
rg -n "System\\.Windows|Wpf\\.Ui|Microsoft\\.Win32|AutoUpdaterDotNET|DispatcherTimer|BitmapImage|pack://application|explorer\\.exe|UseWPF|net10\\.0-windows" iLearn iLearn.Tests
```

Expected: no output.

- [ ] **Step 6: Build clean**

Run:

```bash
dotnet build iLearn.sln
```

Expected: PASS.

- [ ] **Step 7: Run tests**

Run:

```bash
dotnet test iLearn.sln
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add -u iLearn iLearn.Tests
git commit -m "refactor: remove wpf-only implementation"
```

---

### Task 11: Polish Download UX and In-App Feedback

**Files:**
- Modify: `iLearn/Views/Pages/VideoDownloadListPage.axaml`
- Modify: `iLearn/Views/Pages/DownloadManagePage.axaml`
- Modify: `iLearn/Views/Pages/SettingPage.axaml`
- Modify: `iLearn/Downloads/DownloadQueueService.cs`
- Modify: `iLearn/Notifications/NotificationService.cs`

- [ ] **Step 1: Add derived download summary properties**

In `DownloadQueueService`, add:

```csharp
public int ActiveCount => Tasks.Count(item => item.Status == DownloadTaskStatus.Downloading);
public int FailedCount => Tasks.Count(item => item.Status == DownloadTaskStatus.Failed);
public int CompletedCount => Tasks.Count(item => item.Status == DownloadTaskStatus.Completed);
public double TotalBytesPerSecond => Tasks
    .Where(item => item.Status == DownloadTaskStatus.Downloading)
    .Sum(item => item.BytesPerSecond);
```

- [ ] **Step 2: Add clear completed and retry failed commands**

Add to `DownloadQueueService`:

```csharp
public void ClearCompleted()
{
    foreach (var item in _tasks.Where(item => item.Status == DownloadTaskStatus.Completed).ToList())
        _tasks.Remove(item);
}

public async Task RetryFailedAsync(CancellationToken cancellationToken = default)
{
    var failed = _tasks.Where(item => item.Status == DownloadTaskStatus.Failed).Select(item => item.Id).ToList();
    foreach (var id in failed)
        await RetryAsync(id, cancellationToken);
}
```

Add to `DownloadManageViewModel`:

```csharp
[RelayCommand]
private void ClearCompleted()
{
    _downloadQueue.ClearCompleted();
    _notifications.Show("已清理", "已移除完成的下载记录", AppNotificationKind.Success);
}

[RelayCommand]
private async Task RetryFailed()
{
    await _downloadQueue.RetryFailedAsync();
    _notifications.Show("正在重试", "失败任务已重新加入队列", AppNotificationKind.Info);
}
```

- [ ] **Step 3: Improve download manager header controls**

In `DownloadManagePage.axaml`, add these buttons next to "打开下载文件夹":

```xml
<StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Spacing="8">
  <Button Content="重试失败" Command="{Binding RetryFailedCommand}" />
  <Button Content="清理完成" Command="{Binding ClearCompletedCommand}" />
  <Button Content="打开下载文件夹" Command="{Binding OpenDownloadsFolderCommand}" />
</StackPanel>
```

- [ ] **Step 4: Add empty states**

In `DownloadManagePage.axaml`, add above the `ListBox`:

```xml
<Border Grid.Row="2"
        IsVisible="{Binding !Downloads.Count}"
        Padding="28"
        Background="#FFFFFF"
        BorderBrush="#E5E7EB"
        BorderThickness="1">
  <StackPanel HorizontalAlignment="Center" Spacing="8">
    <TextBlock Text="还没有下载任务" FontSize="18" FontWeight="SemiBold" />
    <TextBlock Text="在课程下载里选择 HDMI 或教师画面后，这里会显示进度。" Foreground="#64748B" />
  </StackPanel>
</Border>
```

Keep the `ListBox` visible when `Downloads.Count` is greater than zero.

- [ ] **Step 5: Add download preparation feedback**

In `VideoDownloadListPage.axaml`, bind the button content and progress:

```xml
<Button DockPanel.Dock="Right"
        Content="{Binding IsPreparingDownloads, Converter={StaticResource BoolToDownloadButtonText}}"
        Command="{Binding DownloadSelectedCommand}"
        IsEnabled="{Binding !IsPreparingDownloads}" />
<ProgressBar Grid.Row="2"
             IsIndeterminate="True"
             IsVisible="{Binding IsPreparingDownloads}" />
```

If there is no converter infrastructure, use a second text property in `VideoDownloadListViewModel`:

```csharp
public string DownloadButtonText => IsPreparingDownloads ? "正在准备..." : "加入下载队列";

partial void OnIsPreparingDownloadsChanged(bool value)
{
    OnPropertyChanged(nameof(DownloadButtonText));
}
```

- [ ] **Step 6: Add actionable error notifications**

In `DownloadQueueService` failure catch, include the failed file name:

```csharp
Upsert(DownloadTaskSnapshot.FromRequest(
    request,
    DownloadTaskStatus.Failed,
    $"{request.FileName}: {ex.Message}"));
```

In `DownloadManageViewModel`, when `FailedCount` increases, show:

```csharp
_notifications.Show("下载失败", "有任务下载失败，可在下载管理中重试", AppNotificationKind.Error);
```

- [ ] **Step 7: Verify UX by running app**

Run:

```bash
dotnet run --project iLearn/iLearn.csproj
```

Expected:
- Login button shows immediate busy feedback.
- Course page has visible empty/loading/error states.
- Download selection shows selected count and disabled busy button while preparing.
- Download manager shows queued/downloading/failed/completed status.
- Failed downloads expose retry.
- Completed downloads can be cleared.
- Opening folders works without `explorer.exe`.

- [ ] **Step 8: Commit**

```bash
git add iLearn/Downloads iLearn/ViewModels/Pages iLearn/Views/Pages iLearn/Notifications
git commit -m "feat: polish download and feedback ux"
```

---

### Task 12: Cross-Platform Publish and Documentation

**Files:**
- Modify: `README.md`
- Create: `iLearn/Assets/update-manifest.json`
- Create: `.github/workflows/avalonia-desktop.yml`

- [ ] **Step 1: Add update manifest**

Create `iLearn/Assets/update-manifest.json`:

```json
{
  "version": "1.3.0",
  "notes": "Avalonia cross-platform desktop release.",
  "downloads": {
    "win-x64": "https://github.com/wzyyyyyyy/iLearn/releases/latest/download/iLearn-win-x64.zip",
    "osx-x64": "https://github.com/wzyyyyyyy/iLearn/releases/latest/download/iLearn-osx-x64.zip",
    "osx-arm64": "https://github.com/wzyyyyyyy/iLearn/releases/latest/download/iLearn-osx-arm64.zip",
    "linux-x64": "https://github.com/wzyyyyyyy/iLearn/releases/latest/download/iLearn-linux-x64.tar.gz"
  }
}
```

- [ ] **Step 2: Add GitHub Actions release build**

Create `.github/workflows/avalonia-desktop.yml`:

```yaml
name: avalonia-desktop

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    strategy:
      matrix:
        rid: [win-x64, osx-x64, osx-arm64, linux-x64]
        include:
          - rid: win-x64
            os: windows-latest
          - rid: osx-x64
            os: macos-latest
          - rid: osx-arm64
            os: macos-latest
          - rid: linux-x64
            os: ubuntu-latest
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore iLearn.sln
      - run: dotnet test iLearn.sln --configuration Release --no-restore
      - run: dotnet publish iLearn/iLearn.csproj -c Release -r ${{ matrix.rid }} --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/iLearn-${{ matrix.rid }}
      - uses: actions/upload-artifact@v4
        with:
          name: iLearn-${{ matrix.rid }}
          path: artifacts/iLearn-${{ matrix.rid }}
```

- [ ] **Step 3: Update README positioning**

Replace README description lines mentioning Fluent UI with:

```markdown
# iLearn - 一款现代化的“学在吉大”跨平台客户端

### 基于 Avalonia、Semi.Avalonia 和 Ursa 的 Windows / macOS / Linux 桌面客户端。
```

Update core features:

```markdown
- **跨平台桌面体验：** 使用 Avalonia 构建，支持 Windows、macOS、Linux。
- **顺手的下载管理：** 支持队列、进度、暂停、取消、失败重试、下载速度统计和下载目录快捷打开。
- **清晰的状态提示：** 登录、加载课程、准备下载、检查更新都会显示即时反馈，不再出现点击后无反应。
```

- [ ] **Step 4: Run publish smoke commands locally**

Run on current Mac:

```bash
dotnet publish iLearn/iLearn.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/iLearn-osx-arm64
dotnet publish iLearn/iLearn.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/iLearn-osx-x64
dotnet publish iLearn/iLearn.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/iLearn-linux-x64
dotnet publish iLearn/iLearn.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o artifacts/iLearn-win-x64
```

Expected: all publish commands complete without compile errors. Platform-specific runtime execution is verified on each OS in CI or manually.

- [ ] **Step 5: Final verification**

Run:

```bash
dotnet restore iLearn.sln
dotnet build iLearn.sln
dotnet test iLearn.sln
rg -n "System\\.Windows|Wpf\\.Ui|Microsoft\\.Win32|AutoUpdaterDotNET|DispatcherTimer|BitmapImage|pack://application|explorer\\.exe|UseWPF|net10\\.0-windows" iLearn iLearn.Tests
```

Expected:
- restore PASS
- build PASS
- tests PASS
- `rg` produces no output

- [ ] **Step 6: Commit**

```bash
git add README.md iLearn/Assets/update-manifest.json .github/workflows/avalonia-desktop.yml
git commit -m "docs: document avalonia cross-platform release"
```

---

## Self-Review

**Spec coverage:** The plan migrates WPF to Avalonia, avoids Fluent UI, chooses Semi.Avalonia + Ursa, rewrites Windows-only update and credential storage, optimizes downloads, adds visible feedback, and verifies Windows/macOS/Linux publishing.

**Placeholder scan:** The plan does not rely on ambiguous markers such as later work without instructions. Every task includes file paths, concrete code, commands, and expected results.

**Type consistency:** Route, notification, download, update, and platform type names are introduced before use. View models consistently depend on `NavigationService`, `INotificationService`, `IPlatformLauncher`, `DownloadQueueService`, and `IUpdateService`.
