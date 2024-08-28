using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CommandRunner.Helpers
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue && booleanValue && parameter is string color)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}