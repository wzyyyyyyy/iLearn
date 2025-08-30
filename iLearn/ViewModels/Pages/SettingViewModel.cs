using AutoUpdaterDotNET;
using iLearn.Models;
using iLearn.Services;
using Microsoft.Win32;
using System.IO;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Pages
{
    public partial class SettingViewModel : ObservableObject
    {
        private readonly AppConfig _appConfig;
        private readonly VideoDownloadService _downloadService;
        private readonly AutoUpdateService _autoUpdateService;
        private string _theme = GetCurrentTheme();

        [ObservableProperty]
        private string _appDescription = "学在吉大桌面客户端";

        [ObservableProperty]
        private string _appVersion = GetAppVersion();

        [ObservableProperty]
        private string _lastChecked = "从未检查";

        [ObservableProperty]
        private int _maxConcurrentDownloads;

        [ObservableProperty]
        private int _chunkCount;

        [ObservableProperty]
        private double _speedLimitMBps;

        [ObservableProperty]
        private string _downloadPath = string.Empty;

        public SettingViewModel(AppConfig appConfig, VideoDownloadService downloadService, AutoUpdateService autoUpdateService)
        {
            _appConfig = appConfig;
            _downloadService = downloadService;
            _autoUpdateService = autoUpdateService;

            MaxConcurrentDownloads = _appConfig.MaxConcurrentDownloads;
            ChunkCount = _appConfig.ChunkCount;
            SpeedLimitMBps = _appConfig.SpeedLimitBytesPerSecond / (1024.0 * 1024.0);
            DownloadPath = _appConfig.DownloadPath;
        }

        public string Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value))
                {
                    ApplyTheme(value);
                }
            }
        }

        partial void OnMaxConcurrentDownloadsChanged(int value)
        {
            if (value >= 1 && value <= 10)
            {
                _appConfig.MaxConcurrentDownloads = value;
                _appConfig.Save();
                _downloadService.UpdateMaxConcurrentDownloads(value);
            }
        }

        partial void OnChunkCountChanged(int value)
        {
            if (value >= 1 && value <= 32)
            {
                _appConfig.ChunkCount = value;
                _appConfig.Save();
                _downloadService.UpdateChunkCount(value);
            }
        }

        partial void OnSpeedLimitMBpsChanged(double value)
        {
            if (value >= 0)
            {
                _appConfig.SpeedLimitBytesPerSecond = (long)(value * 1024 * 1024);
                _appConfig.Save();
                _downloadService.UpdateSpeedLimit(_appConfig.SpeedLimitBytesPerSecond);
            }
        }

        partial void OnDownloadPathChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
            {
                _appConfig.DownloadPath = value;
                _appConfig.Save();
            }
        }

        [RelayCommand]
        private void MaxConcurrentDownloadsValueChanged(NumberBoxValueChangedEventArgs args)
        {
            OnMaxConcurrentDownloadsChanged((int)args.NewValue);
        }

        [RelayCommand]
        private void ChunkCountValueChanged(NumberBoxValueChangedEventArgs args)
        {
            OnChunkCountChanged((int)args.NewValue);
        }

        [RelayCommand]
        private void BrowseDownloadPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择下载文件夹",
                InitialDirectory = DownloadPath
            };

            if (dialog.ShowDialog() == true)
            {
                DownloadPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenDownloadPath()
        {
            if (Directory.Exists(DownloadPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", DownloadPath);
            }
        }

        [RelayCommand]
        private void ResetDownloadSettings()
        {
            MaxConcurrentDownloads = 3;
            ChunkCount = 8;
            SpeedLimitMBps = 0;
            DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "iLearnVideo");
        }

        [RelayCommand]
        private async Task OpenEasterEgg()
        {
            EasterEggController easterEggController = new();
            easterEggController.Launch();
        }

        [RelayCommand]
        private void CheckForUpdates()
        {
            LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AutoUpdater.InstalledVersion = new Version(0, 0, 0, 0); // 强制检查更新
            AutoUpdater.Start("https://raw.githubusercontent.com/wzyyyyyyy/iLearn/refs/heads/master/iLearn/Assets/VersionInfo.xml");
        }

        private static string GetCurrentTheme()
        {
            return ApplicationThemeManager.GetAppTheme() switch
            {
                ApplicationTheme.Light => "Light",
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
                _ => "Light"
            };
        }

        private static void ApplyTheme(string themeName)
        {
            ApplicationTheme theme = themeName switch
            {
                "Light" => ApplicationTheme.Light,
                "Dark" => ApplicationTheme.Dark,
                "HighContrast" => ApplicationTheme.HighContrast,
                _ => ApplicationTheme.Light
            };

            ApplicationThemeManager.Apply(theme);
        }

        private static string GetAppVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}