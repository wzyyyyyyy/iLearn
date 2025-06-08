using CommunityToolkit.Mvvm.Messaging;
using iLearn.Helpers.Messages;
using iLearn.Models;
using iLearn.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    public partial class MediaPage : INavigableView<MediaViewModel>
    {
        public MediaViewModel ViewModel { get; }

        public MediaPage(MediaViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}