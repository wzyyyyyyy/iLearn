using iLearn.Models;
using iLearn.Services;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;

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

        public LoginViewModel(ILearnApiService learnApiService, ISnackbarService snackbarService, AppConfig appConfig)
        {
            _learnApiService = learnApiService ?? throw new ArgumentNullException(nameof(learnApiService));
            _SnackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

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

                if (_appConfig.IsAutoLoginEnabled) { 
                    LoginCommand.Execute(null);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            IsAuthenticationInProgress = true;
            try
            {
                var rel = await _learnApiService.LoginAsync(UserName, UserPassword);

                if (!rel)
                {
                    ShowSnackbar($"登录失败，用户名或密码错误");
                    return;
                }

                if (IsRememberMeEnabled)
                {
                    _appConfig.UserName = UserName;
                    _appConfig.UserPassword = UserPassword;
                    _appConfig.Save();
                }
            }
            catch (Exception ex)
            {
                ShowSnackbar($"登录失败，用户名或密码错误");
            }
            finally
            {
                IsAuthenticationInProgress = false;
            }
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