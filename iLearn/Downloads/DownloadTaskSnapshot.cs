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
}
