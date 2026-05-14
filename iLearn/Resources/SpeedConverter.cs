using System.Globalization;
using System.Windows.Data;

namespace iLearn.Resources
{
    public class SpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double speed)
            {
                if (speed >= 1024 * 1024)
                    return $"{speed / (1024 * 1024):F2} MB/s";
                if (speed >= 1024)
                    return $"{speed / 1024:F2} KB/s";
                return $"{speed:F2} B/s";
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
