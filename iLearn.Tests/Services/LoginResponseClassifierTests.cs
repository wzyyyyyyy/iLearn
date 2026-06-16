using iLearn.Services;
using Xunit;

namespace iLearn.Tests.Services;

public sealed class LoginResponseClassifierTests
{
    [Fact]
    public void Classify_ReturnsWrongPassword_WhenPageOnlyMentionsWechat()
    {
        var html = """
        <html>
          <body>
            <div>用户名或密码有误，请重新输入。</div>
            <footer>可使用微信扫码关注学校服务号</footer>
          </body>
        </html>
        """;

        var result = LoginResponseClassifier.Classify(html);

        Assert.Equal(LoginStepResult.WrongPassword, result);
    }

    [Fact]
    public void Classify_ReturnsNeedWechatCode_WhenRecheckFormExists()
    {
        var html = """
        <html>
          <body>
            <form class="recheck" action="/tpass/recheck">
              <input name="WxCode" />
              <input name="lt" value="LT-1" />
            </form>
          </body>
        </html>
        """;

        var result = LoginResponseClassifier.Classify(html);

        Assert.Equal(LoginStepResult.NeedWechatCode, result);
    }
}
