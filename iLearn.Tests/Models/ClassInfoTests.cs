using iLearn.Models;
using Xunit;

namespace iLearn.Tests.Models;

public sealed class ClassInfoTests
{
    [Fact]
    public void CoverImageUrl_UsesApiCoverBeforeTeacherImage()
    {
        var course = new ClassInfo
        {
            Cover = "https://ilearntec.jlu.edu.cn/course-cover.png",
            TeaImg = "https://ilearntec.jlu.edu.cn/teacher.png"
        };

        Assert.Equal("https://ilearntec.jlu.edu.cn/course-cover.png", course.CoverImageUrl);
    }

    [Fact]
    public void CoverImageUrl_NormalizesRelativeApiPath()
    {
        var course = new ClassInfo { Cover = "/studycenter/image/course-cover.png" };

        Assert.Equal("https://ilearntec.jlu.edu.cn/studycenter/image/course-cover.png", course.CoverImageUrl);
    }

    [Fact]
    public void CoverImageUrl_FallsBackToLocalLogo()
    {
        var course = new ClassInfo();

        Assert.Equal("avares://iLearn/Assets/iLearn.png", course.CoverImageUrl);
    }
}
