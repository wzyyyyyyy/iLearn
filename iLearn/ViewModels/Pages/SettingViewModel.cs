using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Appearance;

namespace iLearn.ViewModels.Pages
{
    public partial class SettingViewModel : ObservableObject
    {
        private string _theme = GetCurrentTheme();

        [ObservableProperty]
        private string _appDescription = "学在吉大桌面客户端";

        [ObservableProperty]
        private string _appVersion = GetAppVersion();

        [ObservableProperty]
        private string _lastChecked = "从未检查";

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

        [RelayCommand]
        private async Task CheckForUpdates()
        {
            await Task.Delay(1500);
            LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //(WIP...)
            MessageBox.Show("您当前使用的已经是最新版本。", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string GetCurrentTheme()
        {
            return ApplicationThemeManager.GetAppTheme() switch
            {
                ApplicationTheme.Light => "Light",
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
            };
        }

        private static void ApplyTheme(string themeName)
        {
            ApplicationTheme theme = themeName switch
            {
                "Light" => ApplicationTheme.Light,
                "Dark" => ApplicationTheme.Dark,
                "HighContrast" => ApplicationTheme.HighContrast
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