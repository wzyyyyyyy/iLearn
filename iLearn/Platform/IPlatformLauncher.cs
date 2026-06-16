namespace iLearn.Platform;

public interface IPlatformLauncher
{
    Task OpenFileAsync(string path, CancellationToken cancellationToken = default);

    Task OpenFolderAsync(string path, CancellationToken cancellationToken = default);

    Task OpenUrlAsync(string url, CancellationToken cancellationToken = default);
}
