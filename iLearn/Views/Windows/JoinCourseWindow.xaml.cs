using iLearn.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace iLearn.Views.Windows
{
    public partial class JoinCourseWindow : FluentWindow
    {
        public JoinCourseViewModel ViewModel { get; }

        public JoinCourseWindow(JoinCourseViewModel viewModel)
        {
            InitializeComponent();
            
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
        }
    }
}