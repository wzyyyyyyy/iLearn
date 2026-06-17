using iLearn.Models;
using Xunit;

namespace iLearn.Tests.Models;

public sealed class CourseResponseParsingTests
{
    [Fact]
    public void TermInfoParse_ThrowsFriendlyError_WhenDataIsNull()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TermInfo.Parse("""{"code":401,"message":"未登录","data":null}"""));

        Assert.Contains("课程服务未返回学期数据", ex.Message);
    }

    [Fact]
    public void ClassInfoParse_ThrowsFriendlyError_WhenDataIsNull()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ClassInfo.Parse("""{"code":401,"message":"未登录","data":null}"""));

        Assert.Contains("课程服务未返回课程数据", ex.Message);
    }
}
