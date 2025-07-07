namespace iLearn.ViewModels.Windows
{
    public partial class VideoPlayerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _htmlContent = "<html><body><h1>视频播放器</h1><p>准备加载内容...</p></body></html>";

        [ObservableProperty]
        private string _windowTitle = "视频播放器";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        public VideoPlayerViewModel()
        {
        }

        [RelayCommand]
        private void LoadDefaultContent()
        {
            StatusMessage = "默认内容已加载";
        }

        [RelayCommand]
        private async Task LoadHtmlContentAsync(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return;

            IsLoading = true;
            StatusMessage = "正在加载内容...";

            try
            {
                HtmlContent = htmlContent;
                StatusMessage = "内容加载完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            IsLoading = true;
            StatusMessage = "正在加载网页...";

            try
            {
                // 这里会通过View的事件处理来实现实际的URL加载
                await Task.Delay(100);
                StatusMessage = $"正在访问: {url}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void OnNavigationCompleted(bool isSuccess, string url)
        {
            IsLoading = false;
            StatusMessage = isSuccess ? $"页面加载完成: {url}" : "页面加载失败";
        }
    }
}