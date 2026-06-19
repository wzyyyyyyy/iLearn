using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using iLearn.Models;
using iLearn.Notifications;
using iLearn.Platform;
using iLearn.Security;
using iLearn.Services;
using iLearn.Views.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.ViewModels.Windows;

public enum LoginStep
{
    Password,
    WechatVerify
}

public sealed partial class LoginViewModel : ObservableObject, IDisposable
{
    private readonly ILearnApiService _learnApiService;
    private readonly INotificationService _notifications;
    private readonly ISecretStore _secretStore;
    private readonly AppConfig _appConfig;
    private readonly IServiceProvider _services;
    private readonly IPlatformLauncher _launcher;
    private System.Timers.Timer? _countdownTimer;
    private int _countdownSeconds;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _userPassword = string.Empty;

    [ObservableProperty]
    private bool _isRememberMeEnabled;

    [ObservableProperty]
    private bool _isAutoLoginEnabled;

    [ObservableProperty]
    private bool _isAuthenticationInProgress;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private LoginStep _currentStep = LoginStep.Password;

    [ObservableProperty]
    private Bitmap? _captchaImage;

    [ObservableProperty]
    private string _imageCaptchaCode = string.Empty;

    [ObservableProperty]
    private string _wechatCode = string.Empty;

    [ObservableProperty]
    private bool _isSendingWechatCode;

    [ObservableProperty]
    private string _sendWechatButtonText = "发送验证码";

    [ObservableProperty]
    private bool _canSendWechatCode = true;

    public LoginViewModel(
        ILearnApiService learnApiService,
        INotificationService notifications,
        ISecretStore secretStore,
        AppConfig appConfig,
        IServiceProvider services,
        IPlatformLauncher launcher)
    {
        _learnApiService = learnApiService;
        _notifications = notifications;
        _secretStore = secretStore;
        _appConfig = appConfig;
        _services = services;
        _launcher = launcher;

        IsAutoLoginEnabled = _appConfig.IsAutoLoginEnabled;
        IsRememberMeEnabled = _appConfig.IsRememberMeEnabled;

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(UserName) or nameof(UserPassword) or nameof(IsAuthenticationInProgress) or nameof(CurrentStep))
            {
                LoginCommand.NotifyCanExecuteChanged();
                SubmitCommand.NotifyCanExecuteChanged();
            }
            if (e.PropertyName is nameof(ImageCaptchaCode) or nameof(WechatCode) or nameof(IsAuthenticationInProgress))
            {
                VerifyWechatCommand.NotifyCanExecuteChanged();
                SubmitCommand.NotifyCanExecuteChanged();
            }
            if (e.PropertyName is nameof(ImageCaptchaCode) or nameof(IsSendingWechatCode) or nameof(CanSendWechatCode))
                SendWechatCodeCommand.NotifyCanExecuteChanged();
        };

        _ = LoadSavedCredentialsAsync();
    }

    public bool IsWechatVerifyStep => CurrentStep == LoginStep.WechatVerify;

    public string SubmitButtonText => CurrentStep == LoginStep.WechatVerify
        ? "提交微信验证码"
        : "验证并登录";

    partial void OnCurrentStepChanged(LoginStep value)
    {
        OnPropertyChanged(nameof(IsWechatVerifyStep));
        OnPropertyChanged(nameof(SubmitButtonText));
        SubmitCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (CurrentStep == LoginStep.WechatVerify)
        {
            await VerifyWechatAsync();
            return;
        }

        await LoginAsync();
    }

    private bool CanSubmit()
    {
        return CurrentStep == LoginStep.WechatVerify
            ? CanVerifyWechat()
            : CanLogin();
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (IsAuthenticationInProgress)
            return;

        IsAuthenticationInProgress = true;
        StatusText = "正在连接统一认证服务...";
        ShowMessage("正在登录", "正在连接统一认证服务", AppNotificationKind.Info);

        try
        {
            var result = await _learnApiService.LoginStep1Async(UserName, UserPassword);
            switch (result)
            {
                case LoginStepResult.Success:
                    await OnLoginSuccessAsync();
                    break;
                case LoginStepResult.NeedWechatCode:
                    CurrentStep = LoginStep.WechatVerify;
                    StatusText = "需要微信二次认证，请先刷新并输入图形验证码";
                    await RefreshCasCaptchaAsync();
                    break;
                case LoginStepResult.WrongPassword:
                    ShowMessage("用户名或密码错误", "请检查后重新输入", AppNotificationKind.Error);
                    StatusText = "用户名或密码错误";
                    break;
                default:
                    ShowMessage("登录失败", "请检查网络或稍后重试", AppNotificationKind.Error);
                    StatusText = "登录失败，请稍后重试";
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowLoginError(ex);
        }
        finally
        {
            IsAuthenticationInProgress = false;
        }
    }

    private bool CanLogin()
    {
        return CurrentStep == LoginStep.Password
            && !IsAuthenticationInProgress
            && !string.IsNullOrWhiteSpace(UserName)
            && !string.IsNullOrWhiteSpace(UserPassword);
    }

    [RelayCommand]
    private async Task RefreshCasCaptchaAsync()
    {
        try
        {
            var bytes = await _learnApiService.GetCasCaptchaBytesAsync();
            await using var stream = new MemoryStream(bytes);
            CaptchaImage = new Bitmap(stream);
            StatusText = "验证码已刷新";
        }
        catch
        {
            ShowMessage("验证码加载失败", "请检查网络后重试", AppNotificationKind.Warning);
            StatusText = "验证码加载失败";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendWechat))]
    private async Task SendWechatCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(ImageCaptchaCode))
        {
            ShowMessage("请输入图形验证码", "发送微信验证码前需要先填写图形验证码", AppNotificationKind.Info);
            return;
        }

        IsSendingWechatCode = true;
        StatusText = "正在发送微信验证码...";

        try
        {
            var result = await _learnApiService.RequestWechatCodeAsync(ImageCaptchaCode);
            switch (result)
            {
                case ILearnApiService.WechatCodeRequestResult.Success:
                    StartCountdown(120);
                    ShowMessage("微信验证码已发送", "请在智慧吉大小程序中查收", AppNotificationKind.Success);
                    StatusText = "微信验证码已发送";
                    break;
                case ILearnApiService.WechatCodeRequestResult.CaptchaError:
                    ShowMessage("图形验证码错误", "请刷新后重新输入", AppNotificationKind.Warning);
                    ImageCaptchaCode = string.Empty;
                    await RefreshCasCaptchaAsync();
                    break;
                case ILearnApiService.WechatCodeRequestResult.TooFrequent:
                    ShowMessage("发送过于频繁", "请稍后再试", AppNotificationKind.Warning);
                    StatusText = "发送过于频繁";
                    break;
                case ILearnApiService.WechatCodeRequestResult.SessionExpired:
                    ShowMessage("认证会话已过期", "请返回账号密码步骤重新登录", AppNotificationKind.Error);
                    StatusText = "认证会话已过期";
                    break;
                default:
                    ShowMessage("发送失败", "请稍后重试", AppNotificationKind.Error);
                    StatusText = "发送失败";
                    break;
            }
        }
        catch
        {
            ShowMessage("网络请求失败", "请检查网络连接后重试", AppNotificationKind.Error);
            StatusText = "网络请求失败";
            await RefreshCasCaptchaAsync();
        }
        finally
        {
            IsSendingWechatCode = false;
        }
    }

    private bool CanSendWechat()
    {
        return CanSendWechatCode
            && !IsSendingWechatCode
            && !string.IsNullOrWhiteSpace(ImageCaptchaCode);
    }

    [RelayCommand(CanExecute = nameof(CanVerifyWechat))]
    private async Task VerifyWechatAsync()
    {
        if (IsAuthenticationInProgress)
            return;

        IsAuthenticationInProgress = true;
        StatusText = "正在验证微信验证码...";

        try
        {
            var result = await _learnApiService.LoginStep2Async(ImageCaptchaCode, WechatCode);
            if (result == LoginStepResult.Success)
            {
                await OnLoginSuccessAsync();
            }
            else
            {
                ShowMessage("验证码错误", "请重新输入微信验证码", AppNotificationKind.Warning);
                WechatCode = string.Empty;
                await RefreshCasCaptchaAsync();
            }
        }
        catch (Exception ex)
        {
            ShowLoginError(ex);
        }
        finally
        {
            IsAuthenticationInProgress = false;
        }
    }

    private bool CanVerifyWechat()
    {
        return !IsAuthenticationInProgress
            && !string.IsNullOrWhiteSpace(ImageCaptchaCode)
            && !string.IsNullOrWhiteSpace(WechatCode);
    }

    [RelayCommand]
    private void BackToPassword()
    {
        CurrentStep = LoginStep.Password;
        ImageCaptchaCode = string.Empty;
        WechatCode = string.Empty;
        StatusText = string.Empty;
        StopCountdown();
    }

    [RelayCommand]
    private void SaveLoginOptions()
    {
        _appConfig.IsRememberMeEnabled = IsRememberMeEnabled;
        _appConfig.IsAutoLoginEnabled = IsAutoLoginEnabled;
        _appConfig.Save();
    }

    [RelayCommand]
    private async Task OpenOfficialSiteAsync()
    {
        await _launcher.OpenUrlAsync("https://ilearntec.jlu.edu.cn/");
    }

    private async Task LoadSavedCredentialsAsync()
    {
        IsRememberMeEnabled = _appConfig.IsRememberMeEnabled;
        IsAutoLoginEnabled = _appConfig.IsAutoLoginEnabled;

        if (_appConfig.UserName is null || !_appConfig.IsRememberMeEnabled)
            return;

        UserName = _appConfig.UserName;
        UserPassword = await _secretStore.ReadSecretAsync("login-password") ?? string.Empty;

        if (_appConfig.IsAutoLoginEnabled && CanLogin())
            await LoginAsync();
    }

    private async Task OnLoginSuccessAsync()
    {
        StopCountdown();

        if (IsRememberMeEnabled)
        {
            _appConfig.UserName = UserName;
            await _secretStore.SaveSecretAsync("login-password", UserPassword);
        }
        else
        {
            _appConfig.UserName = null;
            await _secretStore.DeleteSecretAsync("login-password");
        }

        _appConfig.IsRememberMeEnabled = IsRememberMeEnabled;
        _appConfig.IsAutoLoginEnabled = IsAutoLoginEnabled;
        _appConfig.Save();

        ShowMessage("登录成功", "正在打开主界面", AppNotificationKind.Success);

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var oldWindow = desktop.MainWindow;
            var mainWindow = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            oldWindow?.Close();
        }
    }

    private void StartCountdown(int seconds)
    {
        StopCountdown();
        _countdownSeconds = seconds;
        CanSendWechatCode = false;
        SendWechatButtonText = $"重新发送 ({_countdownSeconds}s)";

        _countdownTimer = new System.Timers.Timer(1000);
        _countdownTimer.Elapsed += (_, _) =>
        {
            _countdownSeconds--;
            Dispatcher.UIThread.Post(() =>
            {
                if (_countdownSeconds <= 0)
                {
                    StopCountdown();
                    SendWechatButtonText = "发送验证码";
                    CanSendWechatCode = true;
                    return;
                }

                SendWechatButtonText = $"重新发送 ({_countdownSeconds}s)";
            });
        };
        _countdownTimer.Start();
    }

    private void StopCountdown()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    private void ShowLoginError(Exception ex)
    {
        var message = ex is TaskCanceledException
            ? "请求超时，请检查网络连接或稍后重试"
            : ex.Message;

        ShowMessage("登录失败", message, AppNotificationKind.Error);
        StatusText = message;
    }

    private void ShowMessage(string title, string message, AppNotificationKind kind)
    {
        _notifications.Show(title, message, kind);
    }

    public void Dispose()
    {
        StopCountdown();
        CaptchaImage?.Dispose();
    }
}
