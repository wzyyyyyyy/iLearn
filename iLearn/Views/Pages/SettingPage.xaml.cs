using iLearn.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// SettingPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPage : Page
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
