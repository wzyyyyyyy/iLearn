using Avalonia.Controls;
using iLearn.ViewModels.Pages;

namespace iLearn.Views.Pages;

public partial class CoursesPage : UserControl
{
    public CoursesPage()
    {
        InitializeComponent();
    }

    public CoursesPage(CoursesViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
