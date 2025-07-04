using iLearn.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace iLearn.Views.Windows
{
    /// <summary>
    /// VideoPlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoPlayerWindow : FluentWindow
    {
        public VideoPlayerWindow(VideoPlayerViewModel videoPlayerViewModel)
        {
            DataContext = videoPlayerViewModel ?? throw new ArgumentNullException(nameof(videoPlayerViewModel));
            InitializeComponent();
        }
    }
}