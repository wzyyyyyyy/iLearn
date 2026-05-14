using iLearn.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// LocalVideoPage.xaml 的交互逻辑
    /// </summary>
    public partial class LocalVideoPage : INavigableView<LocalVideoViewModel>
    {
        public LocalVideoViewModel ViewModel { get; }

        public LocalVideoPage(LocalVideoViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }
    }
}