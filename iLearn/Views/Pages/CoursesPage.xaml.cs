using iLearn.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    public partial class CoursesPage : INavigableView<CoursesViewModel>
    {
        public CoursesViewModel ViewModel { get; }

        public CoursesPage(CoursesViewModel coursesViewModel, ISnackbarService snackbarService)
        {
            InitializeComponent();
            ViewModel = coursesViewModel ?? throw new ArgumentNullException(nameof(coursesViewModel));
            DataContext = ViewModel;
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }
    }
}
