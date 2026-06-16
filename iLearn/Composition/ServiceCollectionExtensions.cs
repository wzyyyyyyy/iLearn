using iLearn.Downloads;
using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.Platform;
using iLearn.Security;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddILearnApp(this IServiceCollection services)
    {
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPlatformLauncher, PlatformLauncher>();
        services.AddSingleton<IDownloadEngine, HttpRangeDownloadEngine>();
        services.AddSingleton<DownloadQueueService>();
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
