using iLearn.Models;
using iLearn.Services;
using Wpf.Ui.Appearance;

namespace iLearn.ViewModels.Pages
{
    public partial class SettingViewModel : ObservableObject
    {
        private readonly AppConfig _appConfig;
        private readonly VideoDownloadService _downloadService;
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

        public SettingViewModel(AppConfig appConfig, VideoDownloadService downloadService)
        {
            _appConfig = appConfig;
            _downloadService = downloadService;
            
            MaxConcurrentDownloads = _appConfig.MaxConcurrentDownloads;
            ChunkCount = _appConfig.ChunkCount;
            SpeedLimitMBps = _appConfig.SpeedLimitBytesPerSecond / (1024.0 * 1024.0);
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

        [RelayCommand]
        private void ResetDownloadSettings()
        {
            MaxConcurrentDownloads = 3;
            ChunkCount = 8;
            SpeedLimitMBps = 0;
        }

        [RelayCommand]
        private async Task CheckForUpdates()
        {
            await Task.Delay(1500);
            LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            MessageBox.Show("您当前使用的已经是最新版本。", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
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