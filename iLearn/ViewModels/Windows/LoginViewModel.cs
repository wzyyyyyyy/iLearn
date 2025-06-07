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

        public LoginViewModel(ILearnApiService learnApiService,ISnackbarService snackbarService)
        {
            _learnApiService = learnApiService ?? throw new ArgumentNullException(nameof(learnApiService));
            _SnackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UserName) ||
                    e.PropertyName == nameof(UserPassword) ||
                    e.PropertyName == nameof(IsAuthenticationInProgress))
                {
                    LoginCommand.NotifyCanExecuteChanged();
                }
            };
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            IsAuthenticationInProgress = true;
            try
            {
                var rel = await _learnApiService.LoginAsync(UserName, UserPassword);

                if (!rel) { 
                    
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