using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class MediaPage : UserControl
{
    public MediaPage()
    {
        InitializeComponent();
    }

    public MediaPage(MediaViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
