using iLearn.Models;
using iLearn.Services;
using iLearn.ViewModels.Pages;
using iLearn.ViewModels.Windows;
using iLearn.Views.Pages;
using iLearn.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace iLearn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static new App Current => (App)Application.Current;

        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();
                services.AddHostedService<ApplicationHostService>();

                // Login window
                services.AddSingleton<LoginWindow>();
                services.AddSingleton<LoginViewModel>();

                //Main window
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();

                //Pages
                services.AddSingleton<CoursesPage>();
                services.AddSingleton<CoursesViewModel>();
                services.AddSingleton<MediaPage>();
                services.AddSingleton<MediaViewModel>();
                services.AddSingleton<SettingPage>();
                services.AddSingleton<SettingViewModel>();
                services.AddTransient<VideoDownloadListPage>();
                services.AddTransient<VideoDownloadListViewModel>();
                services.AddSingleton<DownloadManagePage>();
                services.AddSingleton<DownloadManageViewModel>();
                services.AddSingleton<LocalVideoPage>();
                services.AddSingleton<LocalVideoViewModel>();

                //public values
                services.AddSingleton<List<LiveAndRecordInfo>>();

                //service
                services.AddSingleton(sp =>
                {
                    string configPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "iLearn", "config.json");
                    return new AppConfig(configPath);
                });
                services.AddSingleton<ILearnApiService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();
                services.AddSingleton<VideoDownloadService>();

                services.AddSingleton(sp =>
                {
                    var showActions = new Dictionary<Type, Action>
                    {
                        [typeof(LoginViewModel)] = () =>
                        {
                            var win = sp.GetRequiredService<LoginWindow>();
                            win.Show();
                        },
                        [typeof(MainViewModel)] = () =>
                        {
                            var win = sp.GetRequiredService<MainWindow>();
                            win.Show();
                        },
                    };

                    var closeActions = new Dictionary<Type, Action>();

                    return new WindowsManagerService(showActions, closeActions);
                });
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += DispatcherUnhandledExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            await _host.StartAsync();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        private void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            e.Handled = true;
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                LogException(exception);
            }
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception);
            e.SetObserved();
        }

        private void LogException(Exception exception)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFileName = $"error_{timestamp}.log";
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
            var errorMessage = $"[{DateTime.Now}] {exception.ToString()}\n";

            File.AppendAllText(logFilePath, errorMessage);
        }
    }
}
