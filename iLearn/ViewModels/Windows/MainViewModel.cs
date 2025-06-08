using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Windows
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "iLearn";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = [];

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = [];

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems =
        [
            new MenuItem { Header = "Home", Tag = "tray_home" }
        ];
    }
}
