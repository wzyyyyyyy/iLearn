using iLearn.Models;
using Xunit;

namespace iLearn.Tests;

public sealed class LocalVideoFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "iLearn-tests-" + Guid.NewGuid().ToString("N"));

    public LocalVideoFileTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void FindSubtitlePath_ReturnsSubtitleWithSameBaseName()
    {
        var videoPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        var subtitleDirectory = Path.Combine(_root, "Subtitles");
        var subtitlePath = Path.Combine(subtitleDirectory, "高数_第一讲.vtt");
        Directory.CreateDirectory(subtitleDirectory);
        File.WriteAllText(videoPath, string.Empty);
        File.WriteAllText(subtitlePath, string.Empty);

        var localVideo = LocalVideoFile.FromFileName(videoPath);

        Assert.Equal(subtitlePath, localVideo.FindSubtitlePath(_root));
    }

    [Fact]
    public void GetPartnerVideo_ReturnsNullWhenPairIsMissing()
    {
        var videoPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        File.WriteAllText(videoPath, string.Empty);

        var localVideo = LocalVideoFile.FromFileName(videoPath);

        Assert.Null(localVideo.GetPartnerVideo());
    }
}
