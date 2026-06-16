using Avalonia.Controls;
using iLearn.ViewModels.Windows;

namespace iLearn.Views.Windows;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    public LoginWindow(LoginViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
