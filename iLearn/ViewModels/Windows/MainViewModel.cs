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
                TargetPageType = typeof(Views.Pages.MediaPage),
                MenuItemsSource = new object[]
                {
                    new NavigationViewItemSeparator(),
                    new NavigationViewItem()
                    {
                        Content = "课程下载",
                        Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowDownload16 },
                        TargetPageType = typeof(Views.Pages.VideoDownloadListPage)
                    }
                }
            },
             new NavigationViewItem(){
                Content = "下载管理",
                Icon = new SymbolIcon { Symbol = SymbolRegular.MailArrowDown16 },
                TargetPageType = typeof(Views.Pages.DownloadManagePage),
             }
            ];

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = [
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings16 },
                TargetPageType = typeof(Views.Pages.SettingPage)
            },
            ];

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
