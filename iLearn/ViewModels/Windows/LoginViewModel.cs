using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Windows
{
    public partial class LoginViewModel : ObservableObject
    {
        public string WindowTitle => "用户登录";

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

        public LoginViewModel()
        {
            // 属性变更时刷新命令状态
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
                //LOGIN
                await Task.Delay(5000);
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
    }
}