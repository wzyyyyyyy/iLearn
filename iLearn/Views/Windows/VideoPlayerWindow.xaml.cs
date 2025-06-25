using iLearn.ViewModels.Windows;
using System;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

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

            // 添加事件监听
            Media.MediaOpened += Media_MediaOpened;
            Media.MediaFailed += Media_MediaFailed;
            Media.MediaInitializing += Media_MediaInitializing;
            Media.RenderingVideo += Media_RenderingVideo;

            Loaded += VideoPlayerWindow_Loaded;
        }

        private void VideoPlayerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeMediaPlayer();
        }

        private void Media_MediaInitializing(object sender, Unosquare.FFME.Common.MediaInitializingEventArgs e)
        {
            Console.WriteLine($"媒体初始化中: {e.MediaSource}");
        }

        private void Media_MediaOpened(object sender, Unosquare.FFME.Common.MediaOpenedEventArgs e)
        {
            Console.WriteLine($"媒体已打开: {e.Info?.MediaSource}, 时长: {e.Info?.Duration}");
        }

        private void Media_MediaFailed(object sender, Unosquare.FFME.Common.MediaFailedEventArgs e)
        {
            MessageBox.Show($"媒体加载失败: {e.ErrorException.Message}\n\n{e.ErrorException.StackTrace}",
                "详细错误信息", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Media_RenderingVideo(object sender, Unosquare.FFME.Common.RenderingVideoEventArgs e)
        {
            // 第一帧渲染成功
            if (e.EngineState.FramePosition.Milliseconds == 0)
            {
                Console.WriteLine("成功渲染第一帧视频");
            }
        }

        private async void InitializeMediaPlayer()
        {
            try
            {
                // 确保FFME已经初始化
                if (!Unosquare.FFME.Library.IsInitialized)
                {
                    string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ffmpeg");
                    Console.WriteLine($"FFmpeg路径: {ffmpegPath}");
                    Console.WriteLine($"FFmpeg路径存在: {Directory.Exists(ffmpegPath)}");

                    if (!Directory.Exists(ffmpegPath))
                    {
                        MessageBox.Show($"FFmpeg目录不存在: {ffmpegPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 重设FFmpeg路径并初始化
                    Unosquare.FFME.Library.FFmpegDirectory = ffmpegPath;
                }

                // 确保使用正确的视频路径
                string videoPath = @"C:\Users\WZY\source\repos\sbljj\sbljj\bin\Release\net8.0\videos\2025-05-28.mp4";

                Console.WriteLine($"视频路径: {videoPath}");
                Console.WriteLine($"视频文件存在: {File.Exists(videoPath)}");

                if (!File.Exists(videoPath))
                {
                    MessageBox.Show($"视频文件不存在: {videoPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 打开并播放视频
                await Media.Open(new Uri(videoPath));
                Media.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"播放视频时出错: {ex.Message}\n\n{ex.StackTrace}", "详细错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}