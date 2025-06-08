using iLearn.Services;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace iLearn.ViewModels.Windows
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "iLearn";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = [
             new NavigationViewItem()
            {
                Content = "我的课程",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.CoursesPage)
            },
            new NavigationViewItem()
            {
                Content = "课程视频",
                Icon = new SymbolIcon { Symbol = SymbolRegular.VideoClip24 },
                TargetPageType = typeof(Views.Pages.MediaPage)
            }
            ];

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = [];

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems =
        [
            new MenuItem { Header = "Home", Tag = "tray_home" }
        ];

        private readonly ILearnApiService _iLearnApiService;

        public MainViewModel(ILearnApiService iLearnApiService)
        {
            _iLearnApiService = iLearnApiService ?? throw new ArgumentNullException(nameof(iLearnApiService));
        }
    }
}
