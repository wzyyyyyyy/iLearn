namespace iLearn.Downloads;

public enum DownloadTaskStatus
{
    Waiting,
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
    Cancelled
}
