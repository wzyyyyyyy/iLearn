using Avalonia.Controls;
using iLearn.Navigation;
using iLearn.ViewModels;
using iLearn.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Views.Windows;

public partial class MainWindow : Window
{
    private IServiceProvider? _services;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(ShellViewModel viewModel, NavigationService navigation, IServiceProvider services)
        : this()
    {
        _services = services;
        DataContext = viewModel;
        navigation.RouteChanged += (_, route) => ShowRoute(route);
        ShowRoute(AppRoute.Courses);
    }

    private void ShowRoute(AppRoute route)
    {
        if (_services is null)
        {
            return;
        }

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
