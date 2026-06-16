namespace iLearn.Downloads;

public sealed record DownloadRequest(
    string Id,
    string Url,
    string FileName,
    string OutputDirectory,
    string DisplayName,
    string Perspective);
