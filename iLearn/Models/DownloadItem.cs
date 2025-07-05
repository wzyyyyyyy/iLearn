namespace iLearn.Models
{
    public partial class DownloadItem : ObservableObject
    {
        [ObservableProperty] private string url;
        [ObservableProperty] private string fileName;
        [ObservableProperty] private string outputPath;

        [ObservableProperty] private double progress;
        [ObservableProperty] private string status;
        [ObservableProperty] private string speed;
        [ObservableProperty] private double speedValue;
        [ObservableProperty] private long bytesReceived;
        [ObservableProperty] private long totalBytes;
    }
}