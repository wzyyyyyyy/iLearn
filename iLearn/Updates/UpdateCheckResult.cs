namespace iLearn.Updates;

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    Version LatestVersion,
    string Notes,
    string? DownloadUrl);
