using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class VideoDownloadListPage : UserControl
{
    public VideoDownloadListPage()
    {
        InitializeComponent();
    }

    public VideoDownloadListPage(VideoDownloadListViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
