using iLearn.Models;
using iLearn.Services;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace iLearn.ViewModels.Windows
{
    public enum LoginStep { Password, WechatVerify }

    public partial class LoginViewModel : ObservableObject
    {
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
        private LoginStep _currentStep = LoginStep.Password;

        // 二次认证相关
        [ObservableProperty]
        private BitmapImage? _captchaImage;

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

        private System.Timers.Timer _countdownTimer;
        private int _countdownSeconds;

        private readonly ILearnApiService _learnApiService;
        private readonly ISnackbarService _snackbarService;
        private readonly AppConfig _appConfig;
        private readonly WindowsManagerService _windowsManagerService;
        private readonly IContentDialogService _contentDialogService;

        public LoginViewModel(ILearnApiService learnApiService, ISnackbarService snackbarService, AppConfig appConfig, WindowsManagerService windowsManagerService, IContentDialogService contentDialogService)
        {
            _learnApiService = learnApiService ?? throw new ArgumentNullException(nameof(learnApiService));
            _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _windowsManagerService = windowsManagerService ?? throw new ArgumentNullException(nameof(windowsManagerService));
            _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(UserName) or nameof(UserPassword) or nameof(IsAuthenticationInProgress) or nameof(CurrentStep))
                    LoginCommand.NotifyCanExecuteChanged();
                if (e.PropertyName is nameof(ImageCaptchaCode) or nameof(WechatCode) or nameof(IsAuthenticationInProgress))
                    VerifyWechatCommand.NotifyCanExecuteChanged();
            };

            IsAutoLoginEnabled = _appConfig.IsAutoLoginEnabled;
            IsRememberMeEnabled = _appConfig.IsRememberMeEnabled;

            if (_appConfig.UserPassword != null && _appConfig.UserName != null && _appConfig.IsRememberMeEnabled)
            {
                UserName = _appConfig.UserName;
                UserPassword = _appConfig.UserPassword;

                if (_appConfig.IsAutoLoginEnabled)
                    LoginCommand.Execute(null);
            }
        }

        // ── Step 1: 提交账号密码 ──────────────────────────────

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (IsAuthenticationInProgress) return;
            IsAuthenticationInProgress = true;
            try
            {
                var result = await _learnApiService.LoginStep1Async(UserName, UserPassword);

                switch (result)
                {
                    case LoginStepResult.Success:
                        OnLoginSuccess();
                        break;

                    case LoginStepResult.NeedWechatCode:
                        CurrentStep = LoginStep.WechatVerify;
                        await RefreshCasCaptchaAsync();
                        IsAuthenticationInProgress = false;
                        break;

                    case LoginStepResult.WrongPassword:
                        ShowSnackbar("用户名或密码错误，请重新输入");
                        break;

                    default:
                        _snackbarService.Show(
                            "登录失败",
                            "登录失败，请检查网络或稍后重试",
                            ControlAppearance.Danger,
                            new SymbolIcon(SymbolRegular.CalendarError16),
                            TimeSpan.FromSeconds(8));
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = ShowLoginErrorDialogAsync(ex);
            }
            finally
            {
                IsAuthenticationInProgress = false;
            }
        }

        private bool CanLogin() =>
            CurrentStep == LoginStep.Password &&
            !IsAuthenticationInProgress &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(UserPassword);

        // ── 图形验证码刷新 ────────────────────────────────────

        [RelayCommand]
        private async Task RefreshCasCaptchaAsync()
        {
            try
            {
                var bytes = await _learnApiService.GetCasCaptchaBytesAsync();
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(bytes);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                CaptchaImage = bmp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshCasCaptcha] 获取图形验证码失败: {ex.Message}");
            }
        }

        // ── 发送微信验证码 ────────────────────────────────────

        [RelayCommand(CanExecute = nameof(CanSendWechat))]
        private async Task SendWechatCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(ImageCaptchaCode))
            {
                ShowSnackbar("请先输入图形验证码");
                return;
            }

            IsSendingWechatCode = true;
            try
            {
                var result = await _learnApiService.RequestWechatCodeAsync(ImageCaptchaCode);

                switch (result)
                {
                    case ILearnApiService.WechatCodeRequestResult.Success:
                        StartCountdown(120);
                        ShowInfoSnackbar("微信验证码已发送，请在【智慧吉大】小程序中查收");
                        break;
                    case ILearnApiService.WechatCodeRequestResult.CaptchaError:
                        ShowSnackbar("图形验证码错误，请重新输入");
                        await RefreshCasCaptchaAsync();
                        ImageCaptchaCode = string.Empty;
                        break;
                    case ILearnApiService.WechatCodeRequestResult.TooFrequent:
                        ShowSnackbar("请勿重复获取，稍后再试");
                        break;
                    case ILearnApiService.WechatCodeRequestResult.SessionExpired:
                        _snackbarService.Show("获取验证码失败", "会话已过期，请返回重新登录",
                            ControlAppearance.Danger, new SymbolIcon(SymbolRegular.CalendarError16), TimeSpan.FromSeconds(8));
                        break;
                    default:
                        ShowSnackbar("发送失败，请稍后重试");
                        break;
                }
            }
            catch
            {
                ShowSnackbar("网络请求失败，请检查网络连接");
                await RefreshCasCaptchaAsync();
            }
            finally
            {
                IsSendingWechatCode = false;
            }
        }

        private bool CanSendWechat() => CanSendWechatCode && !IsSendingWechatCode;

        // ── Step 2: 提交微信验证码 ────────────────────────────

        [RelayCommand(CanExecute = nameof(CanVerifyWechat))]
        private async Task VerifyWechatAsync()
        {
            if (IsAuthenticationInProgress) return;
            IsAuthenticationInProgress = true;
            try
            {
                var result = await _learnApiService.LoginStep2Async(ImageCaptchaCode, WechatCode);

                if (result == LoginStepResult.Success)
                {
                    OnLoginSuccess();
                }
                else
                {
                    IsAuthenticationInProgress = false;
                    ShowSnackbar("验证码错误，请重新输入");
                    WechatCode = string.Empty;
                    await RefreshCasCaptchaAsync();
                }
            }
            catch (Exception ex)
            {
                _ = ShowLoginErrorDialogAsync(ex);
            }
            finally
            {
                IsAuthenticationInProgress = false;
            }
        }

        private bool CanVerifyWechat() =>
            !IsAuthenticationInProgress &&
            !string.IsNullOrWhiteSpace(ImageCaptchaCode) &&
            !string.IsNullOrWhiteSpace(WechatCode);

        // ── 返回密码步骤 ──────────────────────────────────────

        [RelayCommand]
        private void BackToPassword()
        {
            CurrentStep = LoginStep.Password;
            ImageCaptchaCode = string.Empty;
            WechatCode = string.Empty;
            StopCountdown();
        }

        // ── 通用 ──────────────────────────────────────────────

        private void OnLoginSuccess()
        {
            if (IsRememberMeEnabled)
            {
                _appConfig.UserName = UserName;
                _appConfig.UserPassword = UserPassword;
                _appConfig.Save();
            }
            _windowsManagerService.Show<MainViewModel>();
            _windowsManagerService.Close<LoginViewModel>();
        }

        private void StartCountdown(int seconds)
        {
            StopCountdown();
            _countdownSeconds = seconds;
            CanSendWechatCode = false;
            _countdownTimer = new System.Timers.Timer(1000);
            _countdownTimer.Elapsed += (s, e) =>
            {
                _countdownSeconds--;
                App.Current.Dispatcher.Invoke(() =>
                    SendWechatButtonText = $"重新发送 ({_countdownSeconds}s)");
                if (_countdownSeconds <= 0)
                {
                    StopCountdown();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SendWechatButtonText = "发送验证码";
                        CanSendWechatCode = true;
                        SendWechatCodeCommand.NotifyCanExecuteChanged();
                    });
                }
            };
            _countdownTimer.Start();
        }

        private void StopCountdown()
        {
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }

        private async Task ShowLoginErrorDialogAsync(Exception ex)
        {
            if (ex is TaskCanceledException tcEx && !tcEx.CancellationToken.IsCancellationRequested)
            {
                await _contentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
                {
                    Title = "登录失败",
                    Content = "请求超时，请检查网络连接或稍后重试。",
                    CloseButtonText = "关闭"
                });
                return;
            }

            var sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine($"类型：{ex.GetType().Name}");
                sb.AppendLine($"消息：{ex.Message}");
                sb.AppendLine($"堆栈：{ex.StackTrace}");
                sb.AppendLine("------");
                ex = ex.InnerException;
            }

            await _contentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
            {
                Title = "登录失败，请尝试联系开发者",
                Content = sb.ToString(),
                CloseButtonText = "关闭"
            });
        }

        public void ShowSnackbar(string message)
        {
            _snackbarService.Show(
                "登录失败",
                message,
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.CalendarError16),
                TimeSpan.FromSeconds(3));
        }

        private void ShowInfoSnackbar(string message)
        {
            _snackbarService.Show(
                "提示",
                message,
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.Info16),
                TimeSpan.FromSeconds(4));
        }

        [RelayCommand]
        private void KeyPress(KeyEventArgs e)
        {
            if (e?.Key != Key.Enter) return;

            if (CurrentStep == LoginStep.Password && CanLogin())
            {
                LoginCommand.Execute(null);
                e.Handled = true;
            }
            else if (CurrentStep == LoginStep.WechatVerify && CanVerifyWechat())
            {
                VerifyWechatCommand.Execute(null);
                e.Handled = true;
            }
        }

        [RelayCommand]
        private void ClickCheckBox()
        {
            _appConfig.IsRememberMeEnabled = IsRememberMeEnabled;
            _appConfig.IsAutoLoginEnabled = IsAutoLoginEnabled;
            _appConfig.Save();
        }
    }
}
