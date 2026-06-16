using System.Diagnostics;
using System.Runtime.InteropServices;

namespace iLearn.Platform;

public sealed class PlatformLauncher : IPlatformLauncher
{
    public Task OpenFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File does not exist.", path);
        }

        cancellationToken.ThrowIfCancellationRequested();
        Launch(path);
        return Task.CompletedTask;
    }

    public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        cancellationToken.ThrowIfCancellationRequested();
        Launch(path);
        return Task.CompletedTask;
    }

    public Task OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Scheme) || uri.IsFile)
        {
            throw new ArgumentException("URL must be absolute.", nameof(url));
        }

        cancellationToken.ThrowIfCancellationRequested();
        Launch(url);
        return Task.CompletedTask;
    }

    private static void Launch(string target)
    {
        ProcessStartInfo startInfo;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { target },
                UseShellExecute = false
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "xdg-open",
                ArgumentList = { target },
                UseShellExecute = false
            };
        }
        else
        {
            throw new PlatformNotSupportedException("Opening files, folders, and URLs is not supported on this platform.");
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException($"Could not launch '{target}'.");
        }
    }
}
