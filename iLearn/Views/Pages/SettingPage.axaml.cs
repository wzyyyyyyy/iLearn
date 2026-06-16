using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class SettingPage : UserControl
{
    public SettingPage()
    {
        InitializeComponent();
    }

    public SettingPage(SettingViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
