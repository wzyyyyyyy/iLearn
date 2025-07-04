using iLearn.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// VideoDownloadListPage.xaml 的交互逻辑
    /// </summary>
    public partial class VideoDownloadListPage : INavigableView<VideoDownloadListViewModel>
    {
        public VideoDownloadListViewModel ViewModel { get; }

        public VideoDownloadListPage(VideoDownloadListViewModel videoDownloadListViewModel)
        {
            InitializeComponent();
            ViewModel = videoDownloadListViewModel ?? throw new ArgumentNullException(nameof(videoDownloadListViewModel));
            DataContext = ViewModel;
        }
    }
}
