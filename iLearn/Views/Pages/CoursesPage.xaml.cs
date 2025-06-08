using iLearn.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Abstractions.Controls;

namespace iLearn.Views.Pages
{
    /// <summary>
    /// CoursesPage.xaml 的交互逻辑
    /// </summary>
    public partial class CoursesPage : Page
    {
        public CoursesPage(CoursesViewModel coursesViewModel)
        {
            InitializeComponent();
            DataContext = coursesViewModel ?? throw new ArgumentNullException(nameof(coursesViewModel));
        }
    }
}
