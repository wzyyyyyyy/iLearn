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
        SelectRoute(_navigation.CurrentRoute);
        _navigation.RouteChanged += (_, route) => SelectRoute(route);
    }

    public ObservableCollection<NavigationItemViewModel> Items { get; }

    public INotificationService Notifications { get; }

    [ObservableProperty]
    private AppRoute _currentRoute = AppRoute.Courses;

    public string CurrentRouteTitle => Items.FirstOrDefault(item => item.Route == CurrentRoute)?.Title ?? "iLearn";

    [RelayCommand]
    private void Navigate(NavigationItemViewModel item)
    {
        _navigation.NavigateTo(item.Route);
    }

    private void SelectRoute(AppRoute route)
    {
        CurrentRoute = route;
        OnPropertyChanged(nameof(CurrentRouteTitle));

        foreach (var item in Items)
            item.IsSelected = item.Route == route;
    }
}
