namespace iLearn.Downloads;

public enum DownloadTaskStatus
{
    Waiting,
    Queued,
    Downloading,
    Cancelling,
    Paused,
    Completed,
    Failed,
    Cancelled
}
