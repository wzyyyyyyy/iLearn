using iLearn.Platform;
using Xunit;

namespace iLearn.Tests.Platform;

public sealed class PlatformLauncherTests
{
    [Fact]
    public async Task OpenFileAsync_ThrowsWhenFileDoesNotExist()
    {
        var launcher = new PlatformLauncher();
        var path = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N"));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            launcher.OpenFileAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenUrlAsync_RejectsRelativeUrl()
    {
        var launcher = new PlatformLauncher();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            launcher.OpenUrlAsync("/relative/path", TestContext.Current.CancellationToken));
    }
}
