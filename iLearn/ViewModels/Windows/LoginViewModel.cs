using iLearn.Models;
using iLearn.Services;
using System.Text;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace iLearn.ViewModels.Windows
{
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

        private readonly ILearnApiService _learnApiService;
        private readonly ISnackbarService _SnackbarService;
        private readonly AppConfig _appConfig;
        private readonly WindowsManagerService _windowsManagerService;
        private readonly IContentDialogService _contentDialogService;

        public LoginViewModel(ILearnApiService learnApiService, ISnackbarService snackbarService, AppConfig appConfig, WindowsManagerService windowsManagerService, IContentDialogService contentDialogService)
        {
            _learnApiService = learnApiService ?? throw new ArgumentNullException(nameof(learnApiService));
            _SnackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _windowsManagerService = windowsManagerService ?? throw new ArgumentNullException(nameof(windowsManagerService));
            _contentDialogService = contentDialogService ?? throw new ArgumentNullException(nameof(contentDialogService));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UserName) ||
                    e.PropertyName == nameof(UserPassword) ||
                    e.PropertyName == nameof(IsAuthenticationInProgress))
                {
                    LoginCommand.NotifyCanExecuteChanged();
                }
            };

            IsAutoLoginEnabled = _appConfig.IsAutoLoginEnabled;
            IsRememberMeEnabled = _appConfig.IsRememberMeEnabled;

            if ((_appConfig.UserPassword != null && _appConfig.UserName != null) && _appConfig.IsRememberMeEnabled)
            {
                UserName = _appConfig.UserName;
                UserPassword = _appConfig.UserPassword;

                if (_appConfig.IsAutoLoginEnabled)
                {
                    LoginCommand.Execute(null);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (IsAuthenticationInProgress) return;

            IsAuthenticationInProgress = true;
            try
            {
                bool success = await _learnApiService.LoginAsync(UserName, UserPassword);

                if (!success)
                {
                    ShowSnackbar("登录失败，用户名或密码错误");
                    return;
                }

                if (IsRememberMeEnabled)
                {
                    _appConfig.UserName = UserName;
                    _appConfig.UserPassword = UserPassword;
                    _appConfig.Save();
                }

                _windowsManagerService.Show<MainViewModel>();
                _windowsManagerService.Close<LoginViewModel>();
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


        private bool CanLogin() =>
            !IsAuthenticationInProgress &&
            !string.IsNullOrWhiteSpace(UserName) &&
            !string.IsNullOrWhiteSpace(UserPassword);

        [RelayCommand]
        private void KeyPress(KeyEventArgs e)
        {
            if (e?.Key == Key.Enter && CanLogin())
            {
                LoginCommand.Execute(null);
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

        public void ShowSnackbar(string message)
        {
            _SnackbarService.Show(
                "登录失败",
                message,
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.CalendarError16),
                TimeSpan.FromSeconds(3)
            );
        }
    }
}