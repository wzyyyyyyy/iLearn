using System.Windows.Input;
using System.Windows.Media.Imaging; 

namespace iLearn
{
    public partial class EasterEggWindow : Window
    {
        public const double BaseSize = 100;

        public EasterEggWindow(double width, double height)
        {
            InitializeComponent();

            this.Img.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/IronMan.png"));

            this.Width = width;
            this.Height = height;
        }

        public FrameworkElement TransformTarget => this.Root;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EasterEggController.RequestExit();
            }
        }
    }
}