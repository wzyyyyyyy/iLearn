using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class DownloadManagePage : UserControl
{
    public DownloadManagePage()
    {
        InitializeComponent();
    }

    public DownloadManagePage(DownloadManageViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
