namespace iLearn.Downloads;

public sealed record DownloadTaskSnapshot(
    string Id,
    string Url,
    string FileName,
    string OutputPath,
    string DisplayName,
    string Perspective,
    DownloadTaskStatus Status,
    long BytesReceived,
    long TotalBytes,
    double BytesPerSecond,
    string? ErrorMessage)
{
    public double Progress => TotalBytes <= 0 ? 0 : Math.Clamp(BytesReceived * 100.0 / TotalBytes, 0, 100);

    public string StatusText => Status switch
    {
        DownloadTaskStatus.Waiting => "等待中",
        DownloadTaskStatus.Queued => "排队中",
        DownloadTaskStatus.Downloading => "下载中",
        DownloadTaskStatus.Cancelling => "正在取消",
        DownloadTaskStatus.Paused => "已暂停",
        DownloadTaskStatus.Completed => "已完成",
        DownloadTaskStatus.Failed => "失败",
        DownloadTaskStatus.Cancelled => "已取消",
        _ => Status.ToString()
    };

    public string SpeedText => Status == DownloadTaskStatus.Downloading && BytesPerSecond > 0
        ? FormatBytesPerSecond(BytesPerSecond)
        : "--";

    public string SizeText => TotalBytes > 0
        ? $"{FormatBytes(BytesReceived)} / {FormatBytes(TotalBytes)}"
        : FormatBytes(BytesReceived);

    public string RemainingText
    {
        get
        {
            if (Status != DownloadTaskStatus.Downloading
                || BytesPerSecond <= 0
                || TotalBytes <= 0
                || BytesReceived >= TotalBytes)
            {
                return "--";
            }

            var remainingBytes = TotalBytes - BytesReceived;
            var remaining = TimeSpan.FromSeconds(remainingBytes / BytesPerSecond);
            return remaining.TotalHours >= 1
                ? $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public bool CanCancel => Status is DownloadTaskStatus.Waiting
        or DownloadTaskStatus.Queued
        or DownloadTaskStatus.Downloading
        or DownloadTaskStatus.Paused;

    public bool CanRetry => Status == DownloadTaskStatus.Failed;

    public bool CanOpen => Status == DownloadTaskStatus.Completed;

    public static DownloadTaskSnapshot FromRequest(
        DownloadRequest request,
        DownloadTaskStatus status,
        string? errorMessage = null)
    {
        return new DownloadTaskSnapshot(
            request.Id,
            request.Url,
            request.FileName,
            Path.Combine(request.OutputDirectory, request.FileName),
            request.DisplayName,
            request.Perspective,
            status,
            0,
            0,
            0,
            errorMessage);
    }

    public static DownloadTaskSnapshot Downloading(
        DownloadRequest request,
        long bytesReceived,
        long totalBytes,
        double bytesPerSecond,
        string? errorMessage)
    {
        return new DownloadTaskSnapshot(
            request.Id,
            request.Url,
            request.FileName,
            Path.Combine(request.OutputDirectory, request.FileName),
            request.DisplayName,
            request.Perspective,
            DownloadTaskStatus.Downloading,
            bytesReceived,
            totalBytes,
            bytesPerSecond,
            errorMessage);
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{FormatNumber(bytesPerSecond / 1024 / 1024)} MB/s";
        if (bytesPerSecond >= 1024)
            return $"{FormatNumber(bytesPerSecond / 1024)} KB/s";
        return $"{bytesPerSecond:0} B/s";
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

        return $"{FormatNumber(size)} {units[unitIndex]}";
    }

    private static string FormatNumber(double value)
    {
        return value % 1 == 0 ? $"{value:0}" : $"{value:0.##}";
    }
}
