using iLearn.Downloads;
using iLearn.Models;
using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.Platform;
using iLearn.Security;
using iLearn.Services;
using iLearn.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddILearnApp(this IServiceCollection services)
    {
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPlatformLauncher, PlatformLauncher>();
        services.AddSingleton(_ =>
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iLearn",
                "config.json");
            return new AppConfig(configPath);
        });
        services.AddSingleton<IDownloadEngine, HttpRangeDownloadEngine>();
        services.AddSingleton<DownloadQueueService>();
        services.AddSingleton<ILearnApiService>();
        services.AddSingleton<List<LiveAndRecordInfo>>();
        services.AddSingleton<VideoDownloadListViewModel>();
        services.AddSingleton<DownloadManageViewModel>();
        services.AddSingleton<SettingViewModel>();
        services.AddSingleton<ISecretStore>(_ =>
        {
            var secretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "iLearn",
                "secrets.json");
            return new FileSecretStore(secretsPath);
        });

        return services;
    }
}
