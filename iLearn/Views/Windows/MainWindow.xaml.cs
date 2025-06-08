using iLearn.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace iLearn.Views.Windows;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
    }
}
