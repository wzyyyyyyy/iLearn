using iLearn.ViewModels.Windows;
using iLearn.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace iLearn.Views.Windows;

public partial class MainWindow : INavigationWindow
{
    public MainWindow(MainViewModel mainViewModel,
        INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
    {
        DataContext = mainViewModel;

        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        SetPageService(navigationViewPageProvider);

        navigationService.SetNavigationControl(RootNavigation);

        Loaded += (sender, e) => Navigate(typeof(CoursesPage));
    }

    #region INavigationWindow methods

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    #endregion INavigationWindow methods

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Make sure that closing this window will begin the process of closing the application.
        Application.Current.Shutdown();
    }

    INavigationView INavigationWindow.GetNavigation()
    {
        throw new NotImplementedException();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}
