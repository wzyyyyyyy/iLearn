using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class LocalVideoPage : UserControl
{
    public LocalVideoPage()
    {
        InitializeComponent();
    }

    public LocalVideoPage(LocalVideoViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
