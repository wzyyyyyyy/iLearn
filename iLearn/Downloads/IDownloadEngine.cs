namespace iLearn.Downloads;

public interface IDownloadEngine
{
    Task DownloadAsync(
        DownloadRequest request,
        string outputPath,
        IProgress<DownloadTaskSnapshot> progress,
        CancellationToken cancellationToken);
}
