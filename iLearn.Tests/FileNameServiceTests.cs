using iLearn.Services;
using Xunit;

namespace iLearn.Tests;

public sealed class FileNameServiceTests
{
    [Fact]
    public void BuildVideoFileName_ReplacesInvalidCharacters_AndAddsPerspective()
    {
        var fileName = FileNameService.BuildVideoFileName("高数/第一讲:导论", "HDMI");

        Assert.Equal("高数_第一讲_导论_HDMI.mp4", fileName);
    }

    [Fact]
    public void BuildSubtitleFileName_UsesResourceName()
    {
        var fileName = FileNameService.BuildSubtitleFileName("高数/第一讲:导论");

        Assert.Equal("高数_第一讲_导论.vtt", fileName);
    }

    [Fact]
    public void SanitizeFileName_TrimsTrailingDotsAndSpaces()
    {
        var fileName = FileNameService.SanitizeFileName(" 高数. ");

        Assert.Equal("高数", fileName);
    }

    [Fact]
    public void SanitizeFileName_GuardsWindowsReservedDeviceNames()
    {
        var fileName = FileNameService.SanitizeFileName("CON");

        Assert.Equal("_CON", fileName);
    }
}
