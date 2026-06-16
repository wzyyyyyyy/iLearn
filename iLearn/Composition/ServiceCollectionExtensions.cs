using iLearn.Navigation;
using iLearn.Notifications;
using iLearn.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddILearnApp(this IServiceCollection services)
    {
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IPlatformLauncher, PlatformLauncher>();

        return services;
    }
}
