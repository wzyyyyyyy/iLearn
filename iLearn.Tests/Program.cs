using iLearn.Services;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=== iLearn CAS 登录诊断测试  ===\n");

var api = new ILearnApiService();
var username = "";
var password = "";

// 支持命令行参数：第一个参数为图形验证码，第二个为微信验证码
var argCaptcha = args.Length > 0 ? args[0] : null;
var argWechat = args.Length > 1 ? args[1] : null;

// ════ Step 1: Submit credentials ════
Console.WriteLine("── Step1: LoginStep1Async ──");
var step1Result = await api.LoginStep1Async(username, password);
Console.WriteLine($"结果: {step1Result}");

if (step1Result != LoginStepResult.NeedWechatCode)
{
    Console.WriteLine($"[失败] Step1 未返回 NeedWechatCode，退出");
    return 1;
}

// ════ Get captcha ════
Console.WriteLine("\n── GetCasCaptchaBytesAsync ──");
byte[] captchaBytes;
try
{
    captchaBytes = await api.GetCasCaptchaBytesAsync();
    var imgPath = Path.Combine(Path.GetTempPath(), "iLearn_captcha.png");
    await File.WriteAllBytesAsync(imgPath, captchaBytes);
    Console.WriteLine($"验证码已保存: {imgPath} ({captchaBytes.Length} bytes)");
    // 尝试用默认程序打开验证码图片
    try { Process.Start(new ProcessStartInfo(imgPath) { UseShellExecute = true }); }
    catch { Console.WriteLine("(无法自动打开图片，请手动查看)"); }
}
catch (Exception ex)
{
    Console.WriteLine($"[失败] 获取验证码异常: {ex.GetType().Name}: {ex.Message}");
    return 1;
}

// ════ Get captcha code ════
var captcha = argCaptcha;
if (string.IsNullOrWhiteSpace(captcha))
{
    Console.Write("输入图形验证码: ");
    try { captcha = Console.ReadLine()?.Trim(); }
    catch { /* redirected stdin */ }
}

if (string.IsNullOrWhiteSpace(captcha))
{
    Console.WriteLine("跳过后续步骤。下次可运行: dotnet run --project iLearn.Tests -- <图形验证码>");
    return 0;
}

// ════ Request WeChat code ════
Console.WriteLine($"\n── RequestWechatCodeAsync(code={captcha}) ──");
var r = await api.RequestWechatCodeAsync(captcha);
Console.WriteLine($"结果: {r}");

if (r == ILearnApiService.WechatCodeRequestResult.Success)
{
    var wx = argWechat;
    if (string.IsNullOrWhiteSpace(wx))
    {
        Console.Write("输入微信验证码: ");
        try { wx = Console.ReadLine()?.Trim(); }
        catch { /* redirected stdin */ }
    }

    if (!string.IsNullOrWhiteSpace(wx))
    {
        Console.WriteLine($"\n── LoginStep2Async(code={captcha}, wx={wx}) ──");
        var s2 = await api.LoginStep2Async(captcha, wx);
        Console.WriteLine($"结果: {s2}");
        Console.WriteLine($"Logined: {api.Logined}");

        if (s2 == LoginStepResult.Success)
            Console.WriteLine("\n[成功!] 登录完成！");
        else
            Console.WriteLine("\n[失败] LoginStep2 未成功");
    }
    else
    {
        Console.WriteLine("跳过Step2。下次可运行: dotnet run --project iLearn.Tests -- <图形验证码> <微信验证码>");
    }
}
else if (r == ILearnApiService.WechatCodeRequestResult.CaptchaError)
{
    Console.WriteLine("[提示] 图形验证码错误，请重新运行并输入正确验证码");
}
else if (r == ILearnApiService.WechatCodeRequestResult.SessionExpired)
{
    Console.WriteLine("[提示] 会话过期");
}

Console.WriteLine("\n按任意键退出...");
try { Console.ReadKey(); } catch { /* redirected stdin */ }
return 0;
