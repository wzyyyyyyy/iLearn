﻿using iLearn.Models;
using iLearn.Services;
using iLearn.ViewModels.Windows;
using iLearn.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows.Threading;
using Wpf.Ui;

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
                services.AddHostedService<ApplicationHostService>();

                // Login window
                services.AddSingleton<LoginWindow>();
                services.AddSingleton<LoginViewModel>();

                services.AddSingleton(sp =>
                {
                    string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                    return new AppConfig(configPath);
                });
                services.AddSingleton<ILearnApiService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += DispatcherUnhandledExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

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
