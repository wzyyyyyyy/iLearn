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
    public void FindSubtitlePath_ReturnsSubtitleWithEquivalentBaseName()
    {
        var videoPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        var subtitleDirectory = Path.Combine(_root, "Subtitles");
        var subtitlePath = Path.Combine(subtitleDirectory, "高数第一讲.vtt");
        Directory.CreateDirectory(subtitleDirectory);
        File.WriteAllText(videoPath, string.Empty);
        File.WriteAllText(subtitlePath, string.Empty);

        var localVideo = LocalVideoFile.FromFileName(videoPath);

        Assert.Equal(subtitlePath, localVideo.FindSubtitlePath(_root));
    }

    [Theory]
    [InlineData("高数.vtt")]
    [InlineData("高数_第二讲.vtt")]
    public void FindSubtitlePath_DoesNotReturnUnsafeFuzzyMatch(string subtitleFileName)
    {
        var videoPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        var subtitleDirectory = Path.Combine(_root, "Subtitles");
        Directory.CreateDirectory(subtitleDirectory);
        File.WriteAllText(videoPath, string.Empty);
        File.WriteAllText(Path.Combine(subtitleDirectory, subtitleFileName), string.Empty);

        var localVideo = LocalVideoFile.FromFileName(videoPath);

        Assert.Null(localVideo.FindSubtitlePath(_root));
    }

    [Fact]
    public void FindSubtitlePath_ReturnsNullWhenFuzzyMatchesAreAmbiguous()
    {
        var videoPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        var subtitleDirectory = Path.Combine(_root, "Subtitles");
        Directory.CreateDirectory(subtitleDirectory);
        File.WriteAllText(videoPath, string.Empty);
        File.WriteAllText(Path.Combine(subtitleDirectory, "高数第一讲.vtt"), string.Empty);
        File.WriteAllText(Path.Combine(subtitleDirectory, "高数-第一讲.srt"), string.Empty);

        var localVideo = LocalVideoFile.FromFileName(videoPath);

        Assert.Null(localVideo.FindSubtitlePath(_root));
    }

    [Fact]
    public void GetPartnerVideo_ReturnsNullWhenPairIsMissing()
    {
        var videoPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        File.WriteAllText(videoPath, string.Empty);

        var localVideo = LocalVideoFile.FromFileName(videoPath);

        Assert.Null(localVideo.GetPartnerVideo());
    }

    [Fact]
    public void GetPartnerVideo_PairsTeacherAliasWithHdmi()
    {
        var teacherPath = Path.Combine(_root, "高数_第一讲_teacher.mp4");
        var hdmiPath = Path.Combine(_root, "高数_第一讲_HDMI.mp4");
        File.WriteAllText(teacherPath, string.Empty);
        File.WriteAllText(hdmiPath, string.Empty);

        var localVideo = LocalVideoFile.FromFileName(teacherPath);

        Assert.Equal(hdmiPath, localVideo.GetPartnerVideo()?.FullPath);
    }
}
