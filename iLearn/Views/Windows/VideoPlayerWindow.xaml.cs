using iLearn.ViewModels.Windows;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;

namespace iLearn.Views.Windows
{
    /// <summary>
    /// VideoPlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoPlayerWindow : FluentWindow
    {
        private VideoPlayerViewModel _viewModel;

        public VideoPlayerWindow(VideoPlayerViewModel videoPlayerViewModel)
        {
            _viewModel = videoPlayerViewModel ?? throw new ArgumentNullException(nameof(videoPlayerViewModel));
            DataContext = _viewModel;
            InitializeComponent();
        }

        private async void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                WebView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
                WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
                WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;

                _viewModel.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == nameof(_viewModel.HtmlContent))
                    {
                        LoadHtmlContentAsync(_viewModel.HtmlContent);
                    }
                };

                LoadHtmlContentAsync(_viewModel.HtmlContent);
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _viewModel.OnNavigationCompleted(e.IsSuccess, WebView.CoreWebView2.Source);
        }

        private void LoadHtmlContentAsync(string htmlContent)
        {
            try
            {
                WebView.CoreWebView2.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                _viewModel.OnNavigationCompleted(false, $"Error: {ex.Message}");
            }
        }

        // 公共方法供外部调用
        public async Task LoadUrlAsync(string url)
        {
            await _viewModel.LoadUrlCommand.ExecuteAsync(url);

            if (WebView.CoreWebView2 != null)
            {
                try
                {
                    WebView.CoreWebView2.Navigate(url);
                }
                catch (Exception ex)
                {
                    _viewModel.OnNavigationCompleted(false, $"Error: {ex.Message}");
                }
            }
        }

        public async Task LoadHtmlAsync(string htmlContent)
        {
            await _viewModel.LoadHtmlContentCommand.ExecuteAsync(htmlContent);
        }
    }
}