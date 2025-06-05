using iLearn.ViewModels.Windows;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace iLearn.Views.Windows;

public partial class LoginWindow : FluentWindow
{
    public LoginWindow()
    {
        InitializeComponent();
        DataContext = new LoginViewModel();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var headerStoryboard = (Storyboard)FindResource("CardLoadStoryboard");
        headerStoryboard.Begin(HeaderPanel);

        var cardStoryboard = ((Storyboard)FindResource("CardLoadStoryboard")).Clone();
        cardStoryboard.Begin(LoginCard);

        UsernameTextBox.Focus();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}