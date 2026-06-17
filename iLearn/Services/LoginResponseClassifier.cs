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

        if (HasJwcRelayCredentials(document))
            return LoginStepResult.Success;

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

    private static bool HasJwcRelayCredentials(HtmlDocument document)
    {
        var username = document.DocumentNode.SelectSingleNode("//input[@id='username']")?.GetAttributeValue("value", string.Empty);
        var password = document.DocumentNode.SelectSingleNode("//input[@id='password']")?.GetAttributeValue("value", string.Empty);
        return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
    }
}
