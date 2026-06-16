using Avalonia.Controls;
using iLearn.Navigation;
using iLearn.ViewModels;

namespace iLearn.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(ShellViewModel viewModel, NavigationService navigation)
        : this()
    {
        DataContext = viewModel;
        navigation.RouteChanged += (_, route) => ShowRoute(route);
        ShowRoute(AppRoute.Courses);
    }

    private void ShowRoute(AppRoute route)
    {
        PageHost.Content = new Border
        {
            Background = Avalonia.Media.Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(28),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = GetRouteTitle(route),
                        FontSize = 22,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = "页面正在迁移到 Avalonia，核心数据和下载逻辑已接入。",
                        Foreground = Avalonia.Media.Brushes.Gray,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private static string GetRouteTitle(AppRoute route)
    {
        return route switch
        {
            AppRoute.Courses => "我的课程",
            AppRoute.Media => "课程视频",
            AppRoute.DownloadSelection => "课程下载",
            AppRoute.Downloads => "下载管理",
            AppRoute.LocalVideos => "本地视频",
            AppRoute.Settings => "设置",
            _ => "iLearn"
        };
    }
}
