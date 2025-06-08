using iLearn.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// CoursesPage.xaml 的交互逻辑
    /// </summary>
    public partial class CoursesPage : Page
    {
        public CoursesPage(CoursesViewModel coursesViewModel, ISnackbarService snackbarService)
        {
            InitializeComponent();
            DataContext = coursesViewModel ?? throw new ArgumentNullException(nameof(coursesViewModel));
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }
    }
}
