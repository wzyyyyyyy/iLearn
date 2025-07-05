using iLearn.ViewModels.Pages;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// DownloadManagePage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadManagePage : INavigableView<DownloadManageViewModel>
    {
        public DownloadManageViewModel ViewModel { get; }

        public DownloadManagePage(DownloadManageViewModel viewModel, ISnackbarService snackbarService)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel ?? throw new ArgumentNullException(nameof(viewModel));
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }
    }
}
