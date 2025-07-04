using iLearn.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// SettingPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPage : INavigableView<SettingViewModel>
    {
        public SettingViewModel ViewModel { get; }
        public SettingPage(SettingViewModel settingViewModel)
        {
            InitializeComponent();
            ViewModel = settingViewModel ?? throw new ArgumentNullException(nameof(settingViewModel));
            DataContext = ViewModel;
        }
    }
}
