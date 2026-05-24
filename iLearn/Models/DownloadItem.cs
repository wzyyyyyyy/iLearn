namespace iLearn.Models
{
    public partial class DownloadItem : ObservableObject
    {
        [ObservableProperty] private string url = string.Empty;
        [ObservableProperty] private string fileName = string.Empty;
        [ObservableProperty] private string outputPath = string.Empty;

        [ObservableProperty] private double progress;
        [ObservableProperty] private string status = string.Empty;
        [ObservableProperty] private string speed = string.Empty;
        [ObservableProperty] private double speedValue;
        [ObservableProperty] private long bytesReceived;
        [ObservableProperty] private long totalBytes;
        [ObservableProperty] private string perspective = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;

        public string SizeText => TotalBytes > 0
            ? $"{FormatBytes(BytesReceived)} / {FormatBytes(TotalBytes)}"
            : FormatBytes(BytesReceived);

        public string RemainingText
        {
            get
            {
                if (SpeedValue <= 0 || TotalBytes <= 0 || BytesReceived < 0 || BytesReceived >= TotalBytes)
                    return "--";

                var remainingBytes = TotalBytes - BytesReceived;
                var remaining = TimeSpan.FromSeconds(remainingBytes / SpeedValue);

                return remaining.TotalHours >= 1
                    ? $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                    : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
            }
        }

        partial void OnBytesReceivedChanged(long value)
        {
            OnPropertyChanged(nameof(SizeText));
            OnPropertyChanged(nameof(RemainingText));
        }

        partial void OnTotalBytesChanged(long value)
        {
            OnPropertyChanged(nameof(SizeText));
            OnPropertyChanged(nameof(RemainingText));
        }

        partial void OnSpeedValueChanged(double value)
        {
            OnPropertyChanged(nameof(SizeText));
            OnPropertyChanged(nameof(RemainingText));
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "0 B";

            string[] units = ["B", "KB", "MB", "GB", "TB"];
            var size = (double)bytes;
            var unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{size:F0} {units[unitIndex]}"
                : $"{size:F2} {units[unitIndex]}";
        }
    }
}
