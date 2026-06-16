using HtmlAgilityPack;

namespace iLearn.Services;

public static class LoginResponseClassifier
{
    public static LoginStepResult Classify(string html)
    {
        if (ContainsWrongPasswordText(html))
            return LoginStepResult.WrongPassword;

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var hasWechatCodeInput = document.DocumentNode.SelectSingleNode("//input[@name='WxCode']") is not null;
        var hasRecheckForm = document.DocumentNode.SelectSingleNode("//form[contains(@class,'recheck') or contains(@action,'recheck')]") is not null;
        var hasRecheckCodeEndpoint = html.Contains("recheckcode", StringComparison.OrdinalIgnoreCase);

        return hasWechatCodeInput || hasRecheckForm || hasRecheckCodeEndpoint
            ? LoginStepResult.NeedWechatCode
            : LoginStepResult.WrongPassword;
    }

    private static bool ContainsWrongPasswordText(string html)
    {
        return html.Contains("用户名或密码错误", StringComparison.OrdinalIgnoreCase)
            || html.Contains("账号或密码错误", StringComparison.OrdinalIgnoreCase)
            || html.Contains("用户名或密码有误", StringComparison.OrdinalIgnoreCase)
            || html.Contains("账号或密码有误", StringComparison.OrdinalIgnoreCase)
            || html.Contains("密码错误", StringComparison.OrdinalIgnoreCase)
            || html.Contains("密码有误", StringComparison.OrdinalIgnoreCase);
    }
}
